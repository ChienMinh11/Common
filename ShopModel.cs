using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopModel
    {
        private readonly IShopIapBrigde _iapBridge;
        private readonly ShopConfig _shopConfig;
        private readonly ShopStorage _shopStorage;
        private readonly Dictionary<string, IShopItemData> _shopItemCache;

        public event Action<string, string> OnPurchaseSuccess;
        public event Action<string, string> OnPurchaseFailed;
        public event Action<string, string> OnPriceUpdated;
        public event Action<IShopItemData,List<IItemReward>> OnRewardsGranted;
        public event Action<string> OnPackResetExternally;
        
        public event Action<string> OnBuySuccessExternal;
        public event Action<List<ResourceRewardCommand>> OnRequestAddResource;

        public ShopModel(IShopIapBrigde iapBridge, IShopSaveAdapter saveAdapter, ShopConfig shopConfig)
        {
            _iapBridge = iapBridge;
            _shopConfig = shopConfig;
            _shopStorage = new ShopStorage(saveAdapter);
            
            _shopItemCache = new Dictionary<string, IShopItemData>();
            foreach (var item in _shopConfig.ShopItems)
            {
                if (string.IsNullOrEmpty(item.ProductID)) continue;

                if (!_shopItemCache.ContainsKey(item.ProductID))
                {
                    _shopItemCache.Add(item.ProductID, item);
                }
                else
                {
                    Debug.LogWarning($"[ShopModel] Phát hiện ID sản phẩm bị trùng hoặc null: {item.ProductID}");
                }
            }

            _iapBridge.OnPurchaseSuccess += HandlePurchaseSuccess;
            _iapBridge.OnPurchaseFailure += HandlePurchaseFailure;
            _iapBridge.OnPriceUpdated += HandlePriceUpdated;
        }

        public IReadOnlyList<IShopItemData> GetShopItems() => _shopConfig.ShopItems;
        
        public bool IsItemOwned(string productId)
        {
            if (_shopItemCache.TryGetValue(productId, out var itemData))
            {
                if (_shopStorage.IsPurchaseActive(productId, itemData))
                {
                    return true;
                }

    
                foreach (var kvp in _shopItemCache)
                {
                    var otherItemData = kvp.Value;
         
                    if (_shopStorage.IsPurchaseActive(otherItemData.ProductID, otherItemData))
                    {
                       
                        if (otherItemData.ExclusiveProductIds != null)
                        {
                            foreach (var exclusiveId in otherItemData.ExclusiveProductIds)
                            {
                                if (exclusiveId == productId)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public string GetLocalizedPrice(string productId) => _iapBridge.GetLocalizedPrice(productId);

        public void BuyItem(string productId)
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

        private void HandlePurchaseSuccess(string productId)
        {
            if (!_shopItemCache.TryGetValue(productId, out var itemData)) return;

            if (itemData.IsOneTimePurchase)
            {
                _shopStorage.AddOneTimePurchase(productId);
            }
            else if (itemData.IsTimeLimited)
            {
                _shopStorage.AddTimeLimitedPurchase(productId);
            }
            OnBuySuccessExternal?.Invoke(productId);
            GrantRewards(itemData.Rewards);

            OnPurchaseSuccess?.Invoke(productId, "Mua hàng thành công!");
            OnRewardsGranted?.Invoke(itemData, itemData.Rewards);
        }
        
        private void HandlePurchaseFailure(string productId, string reason)
        {
            OnPurchaseFailed?.Invoke(productId, reason);
        }

        private void HandlePriceUpdated(string productId, string price)
        {
            OnPriceUpdated?.Invoke(productId, price);
        }

        private void GrantRewards(List<IItemReward> rewards)
        {
            var resourceCommands = new List<ResourceRewardCommand>();

            foreach (var reward in rewards)
            {
                resourceCommands.Add(new ResourceRewardCommand
                {
                    ResourceType = reward.ResourceId, 
                    Amount = reward.Amount,
                    IsInfinite = reward.IsInfiniteReward,
                    DurationInSeconds = reward.InfinityDuration
                });
            }

            if (resourceCommands.Count > 0)
            {
                OnRequestAddResource?.Invoke(resourceCommands);
            }
        }

        public void ResetTimeLimitedPack(string productId)
        {
            _shopStorage.ResetTimeLimitedPurchase(productId);
            OnPackResetExternally?.Invoke(productId);
        }

        public void Dispose()
        {
            _iapBridge.OnPurchaseSuccess -= HandlePurchaseSuccess;
            _iapBridge.OnPurchaseFailure -= HandlePurchaseFailure;
            _iapBridge.OnPriceUpdated -= HandlePriceUpdated;
        }
    }
}