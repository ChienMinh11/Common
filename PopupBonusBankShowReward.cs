using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class PopupBonusBankShowReward : PopupBase
    {
         [Header("UI References")] 
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private Transform rewardGridContainer;
        [SerializeField] private RewardSlotView rewardSlotPrefab;

        [SerializeField] private TweenUI buttonFade;
        [SerializeField] private Button claimButton;

        private RewardDisplayService _rewardDisplayService;
        private IEventService _eventService;
        private IEffectSequenceService _effectSequenceService;

        private readonly List<RewardSlotView> _spawnedSlots = new List<RewardSlotView>();
        private readonly List<RewardSlotView> _activeRewardViews = new List<RewardSlotView>();

        [Inject]
        public void Construct(RewardDisplayService rewardDisplayService, IEventService eventService,
            IEffectSequenceService effectSequenceService)
        {
            _rewardDisplayService = rewardDisplayService;
            _eventService = eventService;
            _effectSequenceService = effectSequenceService;
        }

        protected override void SetPopupName() => PopupName = "PopupBonusBankShowReward";
        protected override void SetCacheable() => IsCache = false;

        protected override bool CheckAutoShow() => true;

        protected override void OnShow()
        {
            foreach (var slot in _spawnedSlots)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
            
            _activeRewardViews.Clear();
            buttonFade.SetDefautCanvasGroup();
            claimButton.interactable = false;

            var data = _rewardDisplayService.CurrentData;
            if (data == null) return;

            if (txtTitle != null) txtTitle.text = data.GetTitle();
            if (txtDescription != null) txtDescription.text = data.GetDescription();

            var rewards = data.GetRewards();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewardSlotPrefab == null || rewardGridContainer == null) continue;

                var reward = rewards[i];
                RewardSlotView slotUI;
            
                if (i < _spawnedSlots.Count)
                {
                    slotUI = _spawnedSlots[i];
                }
                else
                {
                    slotUI = Instantiate(rewardSlotPrefab, rewardGridContainer);
                    _spawnedSlots.Add(slotUI);
                }

                if (slotUI == null) continue;

                slotUI.Setup(reward);
                slotUI.SetDefautScale();
                slotUI.gameObject.SetActive(true);

                _activeRewardViews.Add(slotUI);
            }

            PlayRewardAnimationSequenceAsync().Forget();
        }

        private async UniTaskVoid PlayRewardAnimationSequenceAsync()
        {
            var token = this.destroyCancellationToken;

            if (buttonFade != null)
            {
                buttonFade.KillAllTweens();
                if (buttonFade.Rect != null) buttonFade.Rect.localScale = Vector3.zero;
            }

            var animationTasks = new List<UniTask>();
            foreach (var rewardView in _activeRewardViews)
            {
                if (rewardView != null)
                {
                    animationTasks.Add(rewardView.PlayScaleAnimationAsync());
                    await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), cancellationToken: token);
                }
            }

            if (animationTasks.Count > 0)
            {
                await UniTask.WhenAll(animationTasks);
            }

            if (buttonFade != null)
            {
                await buttonFade.PlayShowAsync(token);
                claimButton.interactable = true;
            }
        }

        private bool CheckShowPrefix(IItemReward reward)
        {
            if (reward.ResourceId == "Gold") return false;
            return true;
        }

        protected override void OnHide()
        {
            _rewardDisplayService.CurrentData?.OnClosePopup();
            _rewardDisplayService.SetContextData(null);
        }

        public async void OnClickClaimAndClose()
        {
            var data = _rewardDisplayService.CurrentData;
            if (data != null)
            {
                PublishRewardClaimedEvents(data);
            }
            HideRootCanvas();
            claimButton.interactable = false;
            if (_effectSequenceService != null)
            {
                await UniTask.WaitUntil(() => !_effectSequenceService.IsProcessing, cancellationToken: this.destroyCancellationToken);
            }
            else
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: this.destroyCancellationToken);
            }

            OnClose();
        }

        private void PublishRewardClaimedEvents(IBaseRewardDisplayData data)
        {
            data.OnRewardsClaimed();
            List<RewardClaimedEventData> rewardDataList = RewardItemDataHelper.FromRewardDisplayData(data);
            _eventService.Publish<List<RewardClaimedEventData>, GameEvent>(
                GameEvent.OnRewardClaimByPopupDisplayReward,
                rewardDataList
            );
        }

        protected override void Unload()
        {
            OnClose();
        }
    }
}
