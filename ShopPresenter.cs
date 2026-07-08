using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopPresenter : IShopPresenter,IDisposable
    {
        private readonly ShopModel _model;
        private readonly List<IShopView> _activeViews = new List<IShopView>();
        private readonly Dictionary<string, (Sprite normalIcon, Sprite infinityIcon)> _rewardIconCache 
            = new Dictionary<string, (Sprite, Sprite)>();
        public event Action<IShopNotificationEventData> OnShopRewardsNotificationRequested;

        public ShopPresenter(ShopModel model)
        {
            _model = model;
            _model.OnPurchaseSuccess += HandlePurchaseSuccess;
            _model.OnPurchaseFailed += HandlePurchaseFailed;
            _model.OnPriceUpdated += HandlePriceUpdated;
            _model.OnRewardsGranted += HandleRewardsGranted;
            _model.OnPackResetExternally += HandlePackResetExternally;
            BuildRewardIconCache();
        }

        public void RegisterView(IShopView view)
        {
            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
                view.Initialize(_model.GetShopItems());
                view.SetBuyItemCallback(OnBuyItemRequested);
                foreach (var item in _model.GetShopItems())
                {
                    string price = _model.GetLocalizedPrice(item.ProductID);
                    if (!string.IsNullOrEmpty(price)) view.UpdatePrice(item.ProductID, price);
                }
            }
        }

        public void UnregisterView(IShopView view)
        {
            if (_activeViews.Contains(view))
            {
                _activeViews.Remove(view);
            }
        }
        private void BuildRewardIconCache()
        {
            _rewardIconCache.Clear();
            var shopItems = _model.GetShopItems();

            foreach (var item in shopItems)
            {
                foreach (var reward in item.Rewards)
                {
                    if (string.IsNullOrEmpty(reward.ResourceId)) continue;
                   
                    _rewardIconCache.TryGetValue(reward.ResourceId, out var currentIcons);
                  
                    if (currentIcons.normalIcon == null && reward.IconReward != null)
                    {
                        currentIcons.normalIcon = reward.IconReward;
                    }
                 
                    if (currentIcons.infinityIcon == null && reward.InfinityRewardIcon != null)
                    {
                        currentIcons.infinityIcon = reward.InfinityRewardIcon;
                    }

                    _rewardIconCache[reward.ResourceId] = currentIcons;
                }
            }
        }
       
        public Sprite GetIconResourceReward(string resourceType, bool isInfinite)
        {
            if (string.IsNullOrEmpty(resourceType)) return null;

            if (_rewardIconCache.TryGetValue(resourceType, out var icons))
            {
                return isInfinite ? icons.infinityIcon : icons.normalIcon;
            }
            return null;
        }

        private void CleanUpDestroyedViews()
        {
            _activeViews.RemoveAll(view => 
                view == null || (view is MonoBehaviour mb && mb == null)
            );
        }

        private void OnBuyItemRequested(string productId)
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews) view.ShowLoadingIndicator(true);
            _model.BuyItem(productId);
        }

        private void HandlePurchaseSuccess(string productId, string message)
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews)
            {
                view.ShowLoadingIndicator(false);
                view.OnPurchaseSuccess(productId);
            }
            RefreshShopItemsUI();
        }

        private void HandlePurchaseFailed(string productId, string reason)
        {
            CleanUpDestroyedViews();
            Debug.LogError($"<color=red>[Shop UI]</color> Mua gói {productId} thất bại! Lý do: {reason}");
            foreach (var view in _activeViews)
            {
                view.ShowLoadingIndicator(false);
                view.OnPurchaseFailed(productId, reason);
            }
        }

        private void HandlePriceUpdated(string productId, string price)
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews) view.UpdatePrice(productId, price);
        }

        private void HandleRewardsGranted(IShopItemData itemData,List<IItemReward> rewards)
        {
            CleanUpDestroyedViews();
            var eventData = new ShopNotificationEventData(itemData, rewards) as IShopNotificationEventData;
            OnShopRewardsNotificationRequested?.Invoke(eventData);
        }

        private void HandlePackResetExternally(string productId)
        {
            CleanUpDestroyedViews();
            RefreshShopItemsUI();
        }

        public bool IsItemOwned(string productId)
        {
            var items = _model.GetShopItems();
            IShopItemData itemData = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].ProductID == productId)
                {
                    itemData = items[i];
                    break;
                }
            }
            if (itemData == null) return false;
            return _model.IsItemOwned(productId);
        }

        public void RefreshShopItemsUI()
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews)
            {
                view.Initialize(_model.GetShopItems());
            }
        }

        public void ResetPackAndRefresh(string productId)
        {
            _model.ResetTimeLimitedPack(productId);
            RefreshShopItemsUI(); 
        }

        public void Dispose()
        {
            _model.OnPurchaseSuccess -= HandlePurchaseSuccess;
            _model.OnPurchaseFailed -= HandlePurchaseFailed;
            _model.OnPriceUpdated -= HandlePriceUpdated;
            _model.OnRewardsGranted -= HandleRewardsGranted;
            _model.OnPackResetExternally -= HandlePackResetExternally;
            _activeViews.Clear();
            _model.Dispose();
        }
      
    }
}