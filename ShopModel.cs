using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopModel
    {
        private readonly IShopIapBrigde _iapBridge;
        private readonly ShopConfig _shopConfig;
        private readonly ShopStorage _shopStorage;
        private readonly Dictionary<ProductID, ShopItemData> _shopItemCache;
        private readonly IEventService  _eventService;

        public event Action<ProductID, string> OnPurchaseSuccess;
        public event Action<ProductID, string> OnPurchaseFailed;
        public event Action<ProductID, string> OnPriceUpdated;
        public event Action<ShopItemData, List<ShopItemReward>> OnRewardsGranted;
        public event Action<ProductID> OnPackResetExternally;

        public ShopModel(IShopIapBrigde iapBridge, ISaveSystem saveSystem, ShopConfig shopConfig,IEventService eventService)
        {
            _iapBridge = iapBridge;
            _shopConfig = shopConfig;
            _eventService = eventService;
            _shopStorage = new ShopStorage(saveSystem);
            
            _shopItemCache = new Dictionary<ProductID, ShopItemData>();
            foreach (var item in _shopConfig.ShopItems)
            {
                if (!_shopItemCache.ContainsKey(item.productID))
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

        public IReadOnlyList<ShopItemData> GetShopItems() => _shopConfig.ShopItems;
        
        public bool IsItemOwned(ProductID productId)
        {
            if (_shopItemCache.TryGetValue(productId, out var itemData))
            {
                return _shopStorage.IsPurchaseActive(productId, itemData);
            }
            return false;
        }

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
            _eventService.Publish<ProductID,SharedEventType>(SharedEventType.RequestBuySuccess, productId);
            GrantRewards(itemData.rewards);

            OnPurchaseSuccess?.Invoke(productId, "Mua hàng thành công!");
            OnRewardsGranted?.Invoke(itemData, itemData.rewards);
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
            var resourceCommands = new List<ResourceRewardCommand>();

            foreach (var reward in rewards)
            {
                resourceCommands.Add(new ResourceRewardCommand
                {
                    ResourceType = (ResourceType)reward.resourceType, 
                    Amount = reward.amount,
                    IsInfinite = reward.isInfinite,
                    DurationInSeconds = reward.infiniteDuration
                });
            
            }

            if (resourceCommands.Count > 0)
            {
                _eventService.Publish<List<ResourceRewardCommand>, SharedEventType>(
                    SharedEventType.RequestAddResource,
                    resourceCommands
                );
            }
            
        }
        public void ResetTimeLimitedPack(ProductID productId)
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