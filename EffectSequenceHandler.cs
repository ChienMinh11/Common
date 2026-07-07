using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class EffectSequenceHandler : MonoBehaviour
    {
        private const string ParentTransformName = "ParentContainer";
        private const string CoinTransformName = "IconGoldHome";
        private const string LiveTransformName = "IconLiveHome";
        private const string ButtonPlayName = "ButtonPlayHome";

        [Header("Effect Pool")] [SerializeField]
        private CoinPool coinPool;

        [SerializeField] private RewardViewPool livesPool;
        [SerializeField] private RewardViewPool rewardPool;

        private IEffectSequenceService _effectSequenceService;
        private IPoolService _poolService;
        private IParticleService _particleService;
        private IAudioService _audioService;
        private TransformRegistry _transformRegistry;
        private IEventService _eventService;
        private IResourceService _resourceService;

        private Transform _parentTransform;
        private Transform _coinTransform;
        private Transform _liveTransform;
        private Transform _buttonPlay;
        
        private IDisposable _rewardSubscription;


        [Inject]
        private void Construct(IEffectSequenceService effectSequenceService,
            TransformRegistry transformRegistry,
            IPoolService poolService,
            IParticleService particleService,
            IAudioService audioService,
            IEventService eventService,
            IResourceService resourceService)
        {
            _effectSequenceService = effectSequenceService;
            _transformRegistry = transformRegistry;
            _poolService = poolService;
            _particleService = particleService;
            _audioService = audioService;
            _eventService = eventService;
            _resourceService = resourceService;
            SetupPool();
            RegEvent();
        }

        private void SetupPool()
        {
            coinPool.SetUpPool(_poolService, _audioService, _particleService);
            rewardPool.SetUpPool(_poolService, _audioService, _particleService);
            livesPool.SetUpPool(_poolService, _audioService, _particleService);
        }

        public void GetTransform()
        {
            if (_transformRegistry != null)
            {
                _parentTransform = _transformRegistry.Get(ParentTransformName);
                _coinTransform = _transformRegistry.Get(CoinTransformName);
                _liveTransform = _transformRegistry.Get(LiveTransformName);
                _buttonPlay = _transformRegistry.Get(ButtonPlayName);
                Debug.Log($"Registed all Transform");
            }
            else
            {
                Debug.Log($"TransformRegistry not found!");
            }
          
        }

        private void RegEvent()
        {
            _rewardSubscription?.Dispose();

            _rewardSubscription = _eventService.Observe<List<RewardClaimedEventData>, GameEvent>(GameEvent.OnRewardClaimByPopupDisplayReward)
                .Subscribe(rewards => OnRewardClaimedAsync(rewards).Forget());
        }
        private async UniTaskVoid OnRewardClaimedAsync(List<RewardClaimedEventData> rewards)
        {
            Debug.Log("OnRewardClaimedAsync called");
            if (rewards == null) return;
            RectTransform parentRect = _parentTransform as RectTransform;
            if (parentRect == null) return;

            Vector2 spawnPos = Vector2.zero;
            List<RewardClaimedEventData> otherRewards = new List<RewardClaimedEventData>();

            foreach (var reward in rewards)
            {
                long exactAmount = reward.Amount;

                if (reward.ResourceType == "Gold")
                {
                    Transform targetTransform = _coinTransform != null ? _coinTransform : _buttonPlay;
                    Vector2 targetPos = Vector2.zero;

                    if (targetTransform != null)
                    {
                        targetPos = parentRect.InverseTransformPoint(targetTransform.position);
                    }

                    bool isFirstCoinArrived = false;

                    var coinCommand = new UIBurstAndGatherCommand(
                        coinPool,
                        coinPool.Config,
                        _parentTransform,
                        spawnPos,
                        targetPos,
                        onCoinArrived: () =>
                        {
                            if (!isFirstCoinArrived)
                            {
                                isFirstCoinArrived = true; 
                                _resourceService.ProcessPendingUpdate("Gold", exactAmount);
                            }
                            _eventService.Publish(GameEvent.OnResourceIconArrived, targetTransform, "Gold");
                        });

                    await _effectSequenceService.PlayAsync(coinCommand);
                }
                else if (reward.ResourceType == "Lives")
                {
                    Transform targetTransform = _liveTransform != null ? _liveTransform : _buttonPlay;
                    Vector2 targetPos = Vector2.zero;
                    if (targetTransform != null)
                    {
                        targetPos = parentRect.InverseTransformPoint(targetTransform.position);
                    }

                    EffectBurstConfig runtimeConfig = Instantiate(livesPool.Config);
                    List<RewardClaimedEventData> liveRewardList = new List<RewardClaimedEventData> { reward };

                    var liveCommand = new UIRewardBurstAndGatherCommand(
                        livesPool,
                        runtimeConfig,
                        _parentTransform,
                        spawnPos,
                        targetPos,
                        liveRewardList,
                        onRewardArrived: (resKey) =>
                        {
                            _resourceService.ProcessPendingUpdate(resKey, exactAmount);
                            _eventService.Publish(GameEvent.OnResourceIconArrived, targetTransform, resKey);
                        });

                    await _effectSequenceService.PlayAsync(liveCommand);
                }
                else
                {
                    otherRewards.Add(reward);
                }
            }

            if (otherRewards.Count > 0)
            {
                Transform generalTargetTransform = _buttonPlay;
                Vector2 generalTargetPos = Vector2.zero;

                if (generalTargetTransform != null)
                {
                    generalTargetPos = parentRect.InverseTransformPoint(generalTargetTransform.position);
                }

                EffectBurstConfig runtimeConfig = Instantiate(rewardPool.Config);

                var rewardCommand = new UIRewardBurstAndGatherCommand(
                    rewardPool,
                    runtimeConfig,
                    _parentTransform,
                    spawnPos,
                    generalTargetPos,
                    otherRewards,
                    onRewardArrived: (resKey) =>
                    {
                        int matchingIndex = otherRewards.FindIndex(r => r.ResourceType == resKey);
                        long exactOtherAmount = matchingIndex >= 0 ? otherRewards[matchingIndex].Amount : 0;
                        _resourceService.ProcessPendingUpdate(resKey, exactOtherAmount);
                        _eventService.Publish(GameEvent.OnResourceIconArrived, generalTargetTransform, resKey);
                    });

                await _effectSequenceService.PlayAsync(rewardCommand);
            }
        }

        [Button]
        private void SetSpeed()
        {
            _effectSequenceService.SetSpeed(2);
        }
        private void OnDestroy()
        {
            _rewardSubscription?.Dispose();
        }
    }
}