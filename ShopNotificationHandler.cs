using System;
using ChieChie.Shop;
using ChieChie.UI.Popups;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
   public class ShopNotificationHandler : MonoBehaviour
    {
        private  IEventService _eventService;
        private  IPopupService _popupService;
        private  RewardDisplayService _rewardDisplayService;

        [Inject]
        public void Construct(IEventService eventService, IPopupService popupService, RewardDisplayService rewardDisplayService)
          
        {
            _eventService = eventService;
            _popupService = popupService;
            _rewardDisplayService = rewardDisplayService;
        }
        

        public void Initialize()
        {
            _eventService.Observe<ShopNotificationEventData, SharedEventType>(SharedEventType.OnShopRewardsNotificationRequested)
                .Subscribe(OnRewardsNotificationRequested)
                .RegisterTo(this.destroyCancellationToken);
            
        }

        private void OnRewardsNotificationRequested(ShopNotificationEventData eventData)
        {
            if (eventData == null) return;
       
            ShowNotificationPopupsAsync(eventData.ItemData, eventData.Rewards).Forget();
        }

        private async UniTaskVoid ShowNotificationPopupsAsync(ShopItemData itemData, System.Collections.Generic.List<ShopItemReward> rewards)
        {
            if (itemData == null) return;

            // 1. Log trạng thái (Giữ nguyên logic cũ của bạn)
            if (rewards != null && rewards.Count > 0)
            {
                var logBuilder = new System.Text.StringBuilder();
                logBuilder.AppendLine("<color=yellow><b>⭐ [KẾT QUẢ ĐÃ NHẬN THƯỞNG SẢN PHẨM] ⭐</b></color>");
                foreach (var reward in rewards)
                {
                    if (reward.isInfiniteReward)
                        logBuilder.AppendLine($"🔹 Vật phẩm vô hạn: <b>{reward.resourceType}</b> | Thời gian: {reward.infiniteDuration} giây.");
                    else
                        logBuilder.AppendLine($"🔹 Tài nguyên: <b>{reward.resourceType}</b> | Số lượng: x{reward.amount}");
                }
                Debug.Log(logBuilder.ToString());
            }

            // 2. Hiển thị Popup thông báo mua thành công gói OneTime / TimeLimited
            if (itemData.isOneTimePurchase || itemData.isTimeLimited)
            {
                if (_popupService != null)
                {
                    string msg = $"Bạn đã mua thành công gói: {itemData.displayName}";
                    bool isOpened = await _popupService.ShowPopup("PopupPurchaseMessage", msg);
                    if (isOpened)
                    {
                        var messagePopup = _popupService.GetPopup<PopupPurchaseMessage>("PopupPurchaseMessage");
                        if (messagePopup != null)
                        {
                            try 
                            {
                                await messagePopup.WaitForClose();
                            }
                            catch (System.OperationCanceledException) 
                            {
                                // Handle cancellation if needed
                            }
                        }
                    }
                }
            }

            // 3. Hiển thị Popup danh sách phần thưởng nhận được
            if (rewards != null && rewards.Count > 0)
            {
                if (_rewardDisplayService != null && _popupService != null)
                {
                    var displayData = new ShopRewardDisplayData(rewards);
                    _rewardDisplayService.SetContextData(displayData);
                    await _popupService.ShowPopup("PopupDisplayReward");
                }
            }
        }
      
    }
}
