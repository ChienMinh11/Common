using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.GamePass;
using UnityEngine;
using VContainer.Unity;

namespace Game.GamePlay
{
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
            AddRewardsToResourceSystem(rewards, isAutoClaim: true, true);
        }

        private void AddRewardsToResourceSystem(List<IItemReward> rewards, bool isAutoClaim, bool delayUpdate = false)
        {
            if (rewards == null || rewards.Count == 0) return;

            string prefix = isAutoClaim ? "[Auto Claim]" : "[Normal Claim]";

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
         
        }

        #endregion
    }
}