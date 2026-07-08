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
        private readonly ShopOfferAvailabilityService _offerAvailabilityService;
        private readonly Dictionary<string, IShopItemData> _shopItemCache;

        public event Action<string, string> OnPurchaseSuccess;
        public event Action<string, string> OnPurchaseFailed;
        public event Action<string, string> OnPriceUpdated;
        public event Action<IShopItemData,List<IItemReward>> OnRewardsGranted;
        public event Action<string> OnPackResetExternally;
        
        public event Action<string> OnBuySuccessExternal;
        public event Action<List<ResourceRewardCommand>> OnRequestAddResource;

        public ShopModel(IShopIapBrigde iapBridge, IShopSaveAdapter saveAdapter, ShopConfig shopConfig, ShopOfferAvailabilityService offerAvailabilityService = null)
        {
            _iapBridge = iapBridge;
            _shopConfig = shopConfig;
            _shopStorage = new ShopStorage(saveAdapter);
            _offerAvailabilityService = offerAvailabilityService ?? new ShopOfferAvailabilityService();
            
            _shopItemCache = new Dictionary<string, IShopItemData>();
            foreach (var item in _shopConfig.ShopItems)
            {
                if (item == null) continue;
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

        public IReadOnlyList<IShopItemData> GetShopItems()
        {
            var visibleItems = new List<(IShopItemData item, int priority, int index)>();
            for (int i = 0; i < _shopConfig.ShopItems.Count; i++)
            {
                var item = _shopConfig.ShopItems[i];
                if (item == null) continue;
                var availabilityStatus = GetAvailabilityStatus(item);
                if (availabilityStatus.ShouldShow)
                {
                    visibleItems.Add((item, _offerAvailabilityService.GetPriority(item), i));
                }
            }

            visibleItems.Sort((left, right) =>
            {
                int priorityComparison = right.priority.CompareTo(left.priority);
                if (priorityComparison != 0) return priorityComparison;
                return left.index.CompareTo(right.index);
            });

            var orderedItems = new List<IShopItemData>(visibleItems.Count);
            foreach (var visibleItem in visibleItems)
            {
                orderedItems.Add(visibleItem.item);
            }

            return orderedItems;
        }

        public IReadOnlyList<IShopItemData> GetConfiguredShopItems() => _shopConfig.ShopItems;
        
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

            var availabilityStatus = GetAvailabilityStatus(itemData);
            if (!availabilityStatus.CanPurchase)
            {
                string reason = string.IsNullOrEmpty(availabilityStatus.Reason)
                    ? "Goi nay hien khong mo ban."
                    : availabilityStatus.Reason;
                OnPurchaseFailed?.Invoke(productId, reason);
                return;
            }

            _iapBridge.BuyProduct(productId);
        }

        public bool IsOfferAvailable(string productId)
        {
            if (!_shopItemCache.TryGetValue(productId, out var itemData)) return false;
            return GetAvailabilityStatus(itemData).IsAvailable;
        }

        public bool TryGetOfferTimeRemaining(string productId, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (!_shopItemCache.TryGetValue(productId, out var itemData)) return false;

            var status = GetAvailabilityStatus(itemData);
            if (!status.HasSchedule || !status.ShowCountdown || !status.IsAvailable || !status.HasEndTime)
            {
                return false;
            }

            remaining = status.TimeRemaining;
            return remaining > TimeSpan.Zero;
        }

        private ShopOfferAvailabilityStatus GetAvailabilityStatus(IShopItemData itemData)
        {
            if (!TryGetRelativeAvailability(itemData, out _))
            {
                return _offerAvailabilityService.GetStatus(itemData);
            }

            DateTime startTimeUtc = GetOrCreateRelativeOfferStartTime(itemData);
            return _offerAvailabilityService.GetStatus(itemData, startTimeUtc);
        }

        private DateTime GetOrCreateRelativeOfferStartTime(IShopItemData itemData)
        {
            string productId = itemData.ProductID;
            if (_shopStorage.TryGetRelativeOfferStartTime(productId, out DateTime savedStartTimeUtc))
            {
                return savedStartTimeUtc;
            }

            var initialStatus = _offerAvailabilityService.GetStatus(itemData);
            DateTime startTimeUtc = initialStatus.HasStartTime ? initialStatus.StartTimeUtc : DateTime.UtcNow;
            if (startTimeUtc.Kind != DateTimeKind.Utc) startTimeUtc = startTimeUtc.ToUniversalTime();

            _shopStorage.SetRelativeOfferStartTime(productId, startTimeUtc);
            return startTimeUtc;
        }

        private static bool TryGetRelativeAvailability(IShopItemData itemData, out IShopOfferAvailability availability)
        {
            availability = (itemData as IShopOfferAvailabilityData)?.OfferAvailability;
            return availability != null
                   && availability.UseSchedule
                   && availability.ScheduleType == ShopScheduleType.RelativeDuration;
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
