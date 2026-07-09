using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using UnityEngine;
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

    public class PassActionMediator : IStartable, IDisposable
    {
        private readonly IPassService _passService;
        private readonly IResourceService _resourceService;
        private readonly IPopupService _popupService;
        private readonly RewardDisplayService _rewardDisplayService;
        private readonly IEventService _eventService;

        public PassActionMediator(IPassService passService, IResourceService resourceService,
            IPopupService popupService,
            IEventService eventService,
            RewardDisplayService rewardDisplayService)
        {
            _passService = passService;
            _resourceService = resourceService;
            _popupService = popupService;
            _eventService = eventService;
            _rewardDisplayService = rewardDisplayService;
        }

        public void Start()
        {
            _passService.OnRewardsClaimed += HandleNormalRewardsClaimed;
            _passService.OnAutoClaimedRewardsProcessed += HandleAutoClaimedRewards;
            _passService.OnAutoClaimNotificationTriggered += HandleAutoClaimNotification;
        }

        public void Dispose()
        {
            _passService.OnRewardsClaimed -= HandleNormalRewardsClaimed;
            _passService.OnAutoClaimedRewardsProcessed -= HandleAutoClaimedRewards;
            _passService.OnAutoClaimNotificationTriggered -= HandleAutoClaimNotification;
        }

        #region Logic Xử Lý Tài Nguyên (Resource)

        private void HandleNormalRewardsClaimed(List<IItemReward> rewards)
        {
            AddRewardsToResourceSystem(rewards, isAutoClaim: false);
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

            List<IItemReward> targetRewards;

            if (!eventData.IsBonusData || !eventData.IsBonusBank)
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
                targetRewards = mergedDict.Values.Cast<IItemReward>().ToList();
            }
            else
            {
                targetRewards = eventData.Rewards;
            }

            if (targetRewards.Count == 0) return;

            var displayData = new AutoClaimRewardDisplayData(targetRewards);
            _rewardDisplayService.EnqueueContextData(displayData);
            if (_popupService is IPopupQueueService queueService)
            {
                queueService.Enqueue(new PopupQueueRequest(
                    popupNameId: "PopupBonusBankShowReward",
                    message: "",
                    priority: 1,
                    closeAndRestore: false
                ));
                queueService.Enqueue(new PopupQueueRequest(
                    popupNameId: "PopupDisplayReward",
                    message: "",
                    priority: 0,
                    closeAndRestore: false
                ));
            }
            else
            {
                _popupService.ShowPopup("PopupDisplayReward").Forget();
            }
        }

        #endregion
    }
}