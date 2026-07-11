using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Game.GamePlay
{
    public class AutoClaimMergedReward : IItemReward
    {
        public string ResourceId { get; set; }
        public long Amount { get; set; }
        public bool IsInfiniteReward { get; set; }
        public float InfinityDuration { get; set; }
        public Sprite IconReward { get; set; }
        public Sprite InfinityRewardIcon { get; set; }
    }

    public class GamePassActionMediator : IStartable, IDisposable
    {
        private readonly IPassService _passService;
        private readonly IResourceService _resourceService;
        private readonly IPopupService _popupService;
        private readonly RewardDisplayService _rewardDisplayService;
        private IDisposable _gamePassViewFirstOpenedSubscription;
        private readonly IEventService _eventService;
        private readonly Queue<RewardPopupRequest> _rewardPopupRequests = new Queue<RewardPopupRequest>();
        private bool _isShowingRewardPopup;

        public GamePassActionMediator(IPassService passService, IEventService eventService,
            IResourceService resourceService,
            IPopupService popupService,
            RewardDisplayService rewardDisplayService)
        {
            _passService = passService;
            _eventService = eventService;
            _resourceService = resourceService;
            _popupService = popupService;
            _rewardDisplayService = rewardDisplayService;
        }

        public void Start()
        {
            _passService.OnRewardsClaimed += HandleNormalRewardsClaimed;
            _passService.OnAutoClaimedRewardsProcessed += HandleAutoClaimedRewards;
            _passService.OnAutoClaimNotificationTriggered += HandleAutoClaimNotification;
            _passService.OnBonusBankClaimNotificationTriggered += HandleBonusBankClaimNotification;
            _gamePassViewFirstOpenedSubscription = _eventService.ObserveEvent(GameEvent.OnGamePassViewShowPopupTutorial)
                .Subscribe(_ => { HandleShowpopupTutorialOnFirstOpened(); });
        }

        public void Dispose()
        {
            _passService.OnRewardsClaimed -= HandleNormalRewardsClaimed;
            _passService.OnAutoClaimedRewardsProcessed -= HandleAutoClaimedRewards;
            _passService.OnAutoClaimNotificationTriggered -= HandleAutoClaimNotification;
            _passService.OnBonusBankClaimNotificationTriggered -= HandleBonusBankClaimNotification;
            _gamePassViewFirstOpenedSubscription?.Dispose();
        }

        #region Logic Xử Lý Tài Nguyên (Resource)

        private void HandleNormalRewardsClaimed(List<IItemReward> rewards, PassRewardSource source)
        {
            bool shouldDelay = (source == PassRewardSource.BonusBank);

            AddRewardsToResourceSystem(rewards, isAutoClaim: false, delayUpdate: shouldDelay);
        }

        private void HandleAutoClaimedRewards(List<IItemReward> rewards)
        {
            AddRewardsToResourceSystem(rewards, isAutoClaim: true, delayUpdate: true);
        }

        private void AddRewardsToResourceSystem(List<IItemReward> rewards, bool isAutoClaim, bool delayUpdate = false)
        {
            if (rewards == null || rewards.Count == 0) return;

            foreach (var reward in rewards)
            {
                if (string.IsNullOrEmpty(reward.ResourceId)) continue;

                if (reward.IsInfiniteReward)
                {
                    TimeSpan duration = TimeSpan.FromSeconds(reward.InfinityDuration);
                    _resourceService.AddInfiniteDuration(reward.ResourceId, duration, delayUpdate);
                }
                else
                {
                    _resourceService.AddResource(reward.ResourceId, reward.Amount, delayUpdate);
                }
            }
        }

        #endregion

        #region Logic Hiển Thị Thông Báo / UI (Notification)

        public void HandleAutoClaimNotification(IPassNotificationEventData eventData)
        {
            if (eventData == null || eventData.Rewards == null || eventData.Rewards.Count == 0) return;

            List<IItemReward> normalRewards = new List<IItemReward>();
            List<IItemReward> bonusBankRewards = new List<IItemReward>();

            if (eventData.IsBonusBank)
            {
                bonusBankRewards = eventData.Rewards;
            }
            else
            {
                var mergedDict = new Dictionary<string, AutoClaimMergedReward>();
                foreach (var r in eventData.Rewards)
                {
                    if (string.IsNullOrEmpty(r.ResourceId)) continue;

                    if (mergedDict.TryGetValue(r.ResourceId, out var existing))
                    {
                        if (r.IsInfiniteReward)
                        {
                            existing.IsInfiniteReward = true;
                            existing.InfinityDuration += r.InfinityDuration;
                        }
                        else
                        {
                            existing.Amount += r.Amount;
                        }
                    }
                    else
                    {
                        mergedDict[r.ResourceId] = new AutoClaimMergedReward
                        {
                            ResourceId = r.ResourceId,
                            Amount = r.Amount,
                            IsInfiniteReward = r.IsInfiniteReward,
                            InfinityDuration = r.InfinityDuration,
                            IconReward = r.IconReward,
                            InfinityRewardIcon = r.InfinityRewardIcon
                        };
                    }
                }

                normalRewards = mergedDict.Values.Cast<IItemReward>().ToList();
            }

            EnqueueRewardPopup(new RewardPopupRequest(eventData.IsBonusBank, normalRewards, bonusBankRewards, false));

        }

        private void EnqueueRewardPopup(RewardPopupRequest request)
        {
            if (request == null || !request.HasRewards) return;

            _rewardPopupRequests.Enqueue(request);
            ProcessRewardPopupQueueAsync().Forget();
        }

        private async UniTaskVoid ProcessRewardPopupQueueAsync()
        {
            if (_isShowingRewardPopup) return;

            _isShowingRewardPopup = true;

            try
            {
                while (_rewardPopupRequests.Count > 0)
                {
                    var request = _rewardPopupRequests.Dequeue();
                    await ShowPopupsRewardAsync(request);
                }
            }
            catch (OperationCanceledException)
            {
                _rewardPopupRequests.Clear();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isShowingRewardPopup = false;

                if (_rewardPopupRequests.Count > 0)
                {
                    ProcessRewardPopupQueueAsync().Forget();
                }
            }
        }

        private async UniTask ShowPopupsRewardAsync(RewardPopupRequest request)
        {
            if (request == null || _rewardDisplayService == null || _popupService == null) return;

            if (request.IsBonusBank && request.BonusBankRewards != null && request.BonusBankRewards.Count > 0)
            {
                var bonusBankDisplayData = new BonusBankRewardDisplayData(request.BonusBankRewards);
                _rewardDisplayService.SetContextData(bonusBankDisplayData);

                bool isOpened = await _popupService.ShowPopup("PopupBonusBankShowReward", "", request.CloseAndRestore);
                if (isOpened)
                {
                    var popupBonusBankShowReward = _popupService.GetPopup<PopupBonusBankShowReward>("PopupBonusBankShowReward");
                    if (popupBonusBankShowReward != null)
                    {
                        await popupBonusBankShowReward.WaitForClose();
                    }
                    else
                    {
                        _rewardDisplayService.SetContextData(null);
                    }
                }
                else
                {
                    _rewardDisplayService.SetContextData(null);
                }
            }

            if (request.NormalRewards != null && request.NormalRewards.Count > 0)
            {
                var normalRewardDisplayData = new AutoClaimRewardDisplayData(request.NormalRewards);
                _rewardDisplayService.SetContextData(normalRewardDisplayData);

                bool isOpened = await _popupService.ShowPopup("PopupDisplayReward");
                if (isOpened)
                {
                    var popupDisplayReward = _popupService.GetPopup<PopupDisplayReward>("PopupDisplayReward");
                    if (popupDisplayReward != null)
                    {
                        await popupDisplayReward.WaitForClose();
                    }
                    else
                    {
                        _rewardDisplayService.SetContextData(null);
                    }
                }
                else
                {
                    _rewardDisplayService.SetContextData(null);
                }
            }

        }

        private void HandleBonusBankClaimNotification(IPassNotificationEventData eventData)
        {
            if (eventData == null || eventData.Rewards == null || eventData.Rewards.Count == 0) return;

            List<IItemReward> targetRewards = new List<IItemReward>();

            if (eventData.IsBonusBank)
            {
                targetRewards = eventData.Rewards;
            }

            EnqueueRewardPopup(new RewardPopupRequest(true, null, targetRewards, true));
        }

        private void HandleShowpopupTutorialOnFirstOpened()
        {
            _popupService.ShowPopup("PopupShowGamePassTutorial", "", true);
        }

        #endregion

        private class RewardPopupRequest
        {
            public bool IsBonusBank { get; }
            public List<IItemReward> NormalRewards { get; }
            public List<IItemReward> BonusBankRewards { get; }
            public bool CloseAndRestore { get; }

            public bool HasRewards =>
                (NormalRewards != null && NormalRewards.Count > 0) ||
                (BonusBankRewards != null && BonusBankRewards.Count > 0);

            public RewardPopupRequest(
                bool isBonusBank,
                List<IItemReward> normalRewards,
                List<IItemReward> bonusBankRewards,
                bool closeAndRestore)
            {
                IsBonusBank = isBonusBank;
                NormalRewards = normalRewards;
                BonusBankRewards = bonusBankRewards;
                CloseAndRestore = closeAndRestore;
            }
        }
    }
}
