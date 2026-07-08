using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Game.GamePlay
{
    public class ShopActionMediator : IStartable, IDisposable
    {
       private readonly IShopService _shopService;
        private readonly IResourceService _resourceService;
        private readonly IPopupService _popupService;
        private readonly RewardDisplayService _rewardDisplayService;
        private readonly TabGroup _tabGroup;

        public ShopActionMediator(
            IShopService shopService, 
            IResourceService resourceService,
            IPopupService popupService,
            RewardDisplayService rewardDisplayService, 
            TabGroup tabGroup)
        {
            _shopService = shopService;
            _resourceService = resourceService;
            _popupService = popupService;
            _rewardDisplayService = rewardDisplayService;
            _tabGroup = tabGroup;
        }

        public void Start()
        {
            if (_shopService == null) return;
            
            _shopService.OnRequestAddResource += HandleRequestAddResource;
            _shopService.OnShopRewardsNotificationRequested += OnRewardsNotificationRequested;
        }

        #region Logic Xử Lý Tài Nguyên (Resource)
        private void HandleRequestAddResource(List<ResourceRewardCommand> commands)
        {
            if (commands == null) return;

            foreach (var cmd in commands)
            {
                if (string.IsNullOrEmpty(cmd.ResourceType)) continue;
                
                bool canDelayUpdate = cmd.ResourceType == "Gold" || cmd.ResourceType == "Lives";
               
                if (cmd.IsInfinite)
                {
                    TimeSpan duration = TimeSpan.FromSeconds(cmd.DurationInSeconds);
                    _resourceService.AddInfiniteDuration(cmd.ResourceType, duration, canDelayUpdate);
                }
                else
                {
                    _resourceService.AddResource(cmd.ResourceType, cmd.Amount, canDelayUpdate);
                }
                
                Debug.Log($"[ShopPostPurchaseHandler] Đã tự động thêm {cmd.Amount} {cmd.ResourceType} vào ResourceService (Infinite: {cmd.IsInfinite}).");
            }
        }
        #endregion

        #region Logic Hiển Thị Thông Báo / UI (Notification)
        private void OnRewardsNotificationRequested(IShopNotificationEventData eventData)
        {
            if (eventData == null) return;
            
            _tabGroup.SmoothScrollToDefaultTab();
            ShowNotificationPopupsAsync(eventData.ItemData, eventData.Rewards).Forget();
        }

        private async UniTaskVoid ShowNotificationPopupsAsync(IShopItemData itemData, List<IItemReward> rewards)
        {
            if (itemData == null) return;
            
            var queueService = _popupService as IPopupQueueService;
            if (queueService == null) return;
            
            if (itemData.IsOneTimePurchase || itemData.IsTimeLimited)
            {
                if (!string.IsNullOrEmpty(itemData.PopupName))
                {
                    queueService.Enqueue(new PopupQueueRequest(itemData.PopupName, "", priority: 1));
                }
            }
            
            if (rewards != null && rewards.Count > 0)
            {
                if (_rewardDisplayService != null)
                {
                    var displayData = new ShopRewardDisplayData(rewards);
                    _rewardDisplayService.SetContextData(displayData);
                    queueService.Enqueue(new PopupQueueRequest("PopupDisplayReward", message: "", priority: 0));
                }
            }

            await UniTask.CompletedTask;
        }
        #endregion

        public void Dispose()
        {
            if (_shopService == null) return;
            
            _shopService.OnRequestAddResource -= HandleRequestAddResource;
            _shopService.OnShopRewardsNotificationRequested -= OnRewardsNotificationRequested;
        }
    }
}