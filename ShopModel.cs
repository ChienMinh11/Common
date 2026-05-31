using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    public class ShopModel
    {
        private readonly IIapBrigde _iapBridge;
        private readonly ShopConfig _shopConfig;
        private readonly ShopStorage _shopStorage;
        private readonly IResourceManager _resourceManager;
        private readonly Dictionary<ProductID, ShopItemData> _shopItemCache;

        public event Action<ProductID, string> OnPurchaseSuccess;
        public event Action<ProductID, string> OnPurchaseFailed;
        public event Action<ProductID, string> OnPriceUpdated;
        public event Action<List<ShopItemReward>> OnRewardsGranted;

        public ShopModel(IIapBrigde iapBridge, SaveSystem saveSystem, ShopConfig shopConfig, IResourceManager resourceManager)
        {
            _iapBridge = iapBridge;
            _shopConfig = shopConfig;
            _resourceManager = resourceManager;
            _shopStorage = new ShopStorage(saveSystem);
            
            _shopItemCache = new Dictionary<ProductID, ShopItemData>();
            foreach (var item in _shopConfig.shopItems)
            {
                if (item.productID != null && !_shopItemCache.ContainsKey(item.productID))
                {
                    _shopItemCache.Add(item.productID, item);
                }
                else
                {
                    Debug.LogWarning($"[ShopModel] Phát hiện ID sản phẩm bị trùng hoặc null: {item.productID}");
                }
            }

            _iapBridge.OnPurchaseSuccess += HandlePurchaseSuccess;
            _iapBridge.OnPurchaseFailure += HandlePurchaseFailure;
            _iapBridge.OnPriceUpdated += HandlePriceUpdated;
        }

        public List<ShopItemData> GetShopItems() => _shopConfig.shopItems;

        public string GetLocalizedPrice(ProductID productId) => _iapBridge.GetLocalizedPrice(productId);

        public void BuyItem(ProductID productId)
        {
            if (!_shopItemCache.TryGetValue(productId, out var itemData))
            {
                OnPurchaseFailed?.Invoke(productId, "Sản phẩm không tồn tại trong cấu hình!");
                return;
            }

            if (_shopStorage.IsPurchaseActive(productId, itemData))
            {
                OnPurchaseFailed?.Invoke(productId, "Bạn đã sở hữu sản phẩm này rồi.");
                return;
            }

            _iapBridge.BuyProduct(productId);
        }

        private void HandlePurchaseSuccess(ProductID productId)
        {
            if (!_shopItemCache.TryGetValue(productId, out var itemData)) return;

            if (itemData.isOneTimePurchase)
            {
                _shopStorage.AddOneTimePurchase(productId);
            }
            else if (itemData.isTimeLimited)
            {
                _shopStorage.AddTimeLimitedPurchase(productId);
            }

            GrantRewards(itemData.rewards);

            OnPurchaseSuccess?.Invoke(productId, "Mua hàng thành công!");
            OnRewardsGranted?.Invoke(itemData.rewards);
        }

        private void HandlePurchaseFailure(ProductID productId, string reason)
        {
            OnPurchaseFailed?.Invoke(productId, reason);
        }

        private void HandlePriceUpdated(ProductID productId, string price)
        {
            OnPriceUpdated?.Invoke(productId, price);
        }

        private void GrantRewards(List<ShopItemReward> rewards)
        {
            foreach (var reward in rewards)
            {
                if (reward.rewardType == RewardType.Resource)
                {
                    if (reward.isInfiniteReward)
                    {
                        // Cộng thời gian vô hạn tài nguyên (ví dụ: Vô hạn tim/mạng)
                        _resourceManager.AddInfiniteDuration(reward.resourceType, TimeSpan.FromSeconds(reward.infinityDuration));
                    }
                    else
                    {
                        // Cộng tài nguyên bình thường (Gold, Coin, v.v.)
                        _resourceManager.AddResource(reward.resourceType, reward.GetAmount());
                    }
                }
                // Bạn có thể xử lý thêm RewardType.NoAds hoặc GoldenPass tại đây nếu cần thiết
            }
        }

        public void Dispose()
        {
            _iapBridge.OnPurchaseSuccess -= HandlePurchaseSuccess;
            _iapBridge.OnPurchaseFailure -= HandlePurchaseFailure;
            _iapBridge.OnPriceUpdated -= HandlePriceUpdated;
        }
    }
}