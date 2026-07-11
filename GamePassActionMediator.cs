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
            List<IItemReward>bonusBankRewards = new List<IItemReward>();

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

            ShowPopupsRewardAsync(eventData, normalRewards, bonusBankRewards).Forget();

        }
        private async UniTaskVoid ShowPopupsRewardAsync(IPassNotificationEventData itemNotificationData, List<IItemReward> normalRewards, List<IItemReward> bonusBankRewards)
        {
            if (itemNotificationData == null) return;
            
            if (itemNotificationData.IsBonusBank)
            {
                if (bonusBankRewards != null && bonusBankRewards.Count > 0)
                {
                    if (_rewardDisplayService == null) return;
                    var bonusBankDisplayData = new BonusBankRewardDisplayData(bonusBankRewards);
                    _rewardDisplayService.EnqueueContextData(bonusBankDisplayData);
                    _rewardDisplayService.SetContextData(bonusBankDisplayData);
                    bool isOpened = await _popupService.ShowPopup("PopupBonusBankShowReward");
                    if (isOpened)
                    {
                        var popupBonusBankShowReward = _popupService.GetPopup<PopupBonusBankShowReward>("PopupBonusBankShowReward");
                        if (popupBonusBankShowReward != null)
                        {
                            await popupBonusBankShowReward.WaitForClose(); 
                        }
                    }
                }
            }
            
            if (normalRewards != null && normalRewards.Count > 0)
            {
              
                if (_rewardDisplayService != null)
                {
                    var normalRewardDisplayData = new AutoClaimRewardDisplayData(normalRewards);
                    _rewardDisplayService.EnqueueContextData(normalRewardDisplayData);
                    _rewardDisplayService.SetContextData( normalRewardDisplayData);
                    await _popupService.ShowPopup("PopupDisplayReward");
                }
            }

            await UniTask.CompletedTask;
        }

        private void HandleBonusBankClaimNotification(IPassNotificationEventData eventData)
        {
            if (eventData == null || eventData.Rewards == null || eventData.Rewards.Count == 0) return;

            List<IItemReward> targetRewards = new List<IItemReward>();

            if (eventData.IsBonusBank)
            {
                targetRewards = eventData.Rewards;
            }

            var displayData = new AutoClaimRewardDisplayData(targetRewards);
            _rewardDisplayService.EnqueueContextData(displayData);
            if (_popupService != null)
            {
                _popupService.ShowPopup("PopupBonusBankShowReward", "", true);
            }
        }

        private void HandleShowpopupTutorialOnFirstOpened()
        {
            _popupService.ShowPopup("PopupShowGamePassTutorial", "", true);
        }

        #endregion
    }
}