using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
    public class ShopView : MonoBehaviour, IShopView
    {
        [Header("Static UI Setup")]
        [Tooltip("Kéo thả tất cả các ShopItemView đã sắp xếp sẵn trong các Layout/Tab ở Editor vào đây")]
        [SerializeField] private List<ShopItemView> staticItemViews = new List<ShopItemView>();

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingOverlay;

        private ShopPresenter _shopPresenter;
        private readonly Dictionary<ProductID, ShopItemView> _activeItems = new Dictionary<ProductID, ShopItemView>();
        private readonly Dictionary<ProductID, ShopItemData> _itemDataCache = new Dictionary<ProductID, ShopItemData>();
        private Action<ProductID> _buyItemCallback;

        [Inject]
        public void Construct(ShopManager shopManager)
        {
            _shopPresenter = shopManager.Presenter;
            Initialize();
        }

        private void Initialize()
        {
            if (_shopPresenter != null)
            {
                _shopPresenter.RegisterView(this);
            }
            else
            {
                Debug.LogError("[ShopView] Không tìm thấy ShopPresenter từ ShopManager!");
            }
        }

        private void OnDestroy()
        {
            if (_shopPresenter != null)
            {
                _shopPresenter.UnregisterView(this);
            }
        }

        public void Initialize(List<ShopItemData> items, ShopPresenter shopPresenter)
        {
            _activeItems.Clear();
            _itemDataCache.Clear();

            foreach (var itemData in items)
            {
                if (itemData.productID == ProductID.NONE || itemData.productID == null) continue;
                
                if (!_itemDataCache.ContainsKey(itemData.productID))
                {
                    _itemDataCache.Add(itemData.productID, itemData);
                }
            }
            foreach (var itemUI in staticItemViews)
            {
                if (itemUI == null) continue;

                ProductID id = itemUI.TargetProductID;
                
                if (_itemDataCache.TryGetValue(id, out var realData))
                {
                    _activeItems.Add(id, itemUI);
                    itemUI.gameObject.SetActive(true); 
                }
                else
                {
                    itemUI.gameObject.SetActive(false);
                    Debug.LogWarning($"[ShopView] Item UI có ID {id} được đặt trong Layout nhưng không tìm thấy cấu hình trong ShopConfig!");
                }
            }
        }

       public void SetBuyItemCallback(Action<ProductID> callback)
       {
           _buyItemCallback = callback;
       
           foreach (var kvp in _activeItems) 
           {
               var productId = kvp.Key; 
               var itemUI = kvp.Value; 
       
               if (_itemDataCache.TryGetValue(productId, out var realData)) 
               {
                  
                   itemUI.Setup(realData, _shopPresenter, _buyItemCallback); 
               }
           }
       }
        public void UpdatePrice(ProductID itemId, string price)
        {
            if (_activeItems.TryGetValue(itemId, out var itemUI))
            {
                itemUI.UpdatePriceText(price);
            }
        }

        public void OnPurchaseSuccess(ProductID itemId)
        {
            var itemData = GetItemData(itemId);
            string packName = itemData != null ? itemData.displayName : itemId.ToString();
            Debug.Log($"<color=green>[Shop UI]</color> Đã mua thành công gói: <b>{packName}</b>!");
        }

        public void OnPurchaseFailed(ProductID itemId, string reason)
        {
            Debug.LogError($"<color=red>[Shop UI]</color> Mua gói {itemId} thất bại! Lý do: {reason}");
        }

        public void ShowLoadingIndicator(bool show)
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(show);
            }
        }

        public void ShowRewardsNotification(List<ShopItemReward> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;

            StringBuilder logBuilder = new StringBuilder();
            logBuilder.AppendLine("<color=yellow><b>⭐ [KẾT QUẢ ĐÃ NHẬN THƯỞNG SẢN PHẨM] ⭐</b></color>");
            
            foreach (var reward in rewards)
            {
                if (reward.isInfiniteReward)
                {
                    logBuilder.AppendLine($"🔹 Vật phẩm vô hạn: <b>{reward.resourceType}</b> | Thời gian duy trì: {reward.infinityDuration} giây.");
                }
                else
                {
                    logBuilder.AppendLine($"🔹 Tài nguyên cộng thêm: <b>{reward.resourceType}</b> | Số lượng: x{reward.amount}");
                }
            }

            Debug.Log(logBuilder.ToString());
        }

        private ShopItemData GetItemData(ProductID id)
        {
            return _itemDataCache.TryGetValue(id, out var data) ? data : null;
        }
    }
}