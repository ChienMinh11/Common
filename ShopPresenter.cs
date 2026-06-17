using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopPresenter : IDisposable
    {
        private readonly ShopModel _model;
        private readonly List<IShopView> _activeViews = new List<IShopView>();
        private readonly IIconProvider  _iconProvider;
        private readonly IEventService _eventService;
        private readonly RewardDisplayService _rewardDisplayService;

        public ShopPresenter(ShopModel model, IIconProvider iconProvider, IEventService eventService, RewardDisplayService rewardDisplayService)
        {
            _model = model;
            _iconProvider = iconProvider;
            _eventService = eventService;
            _rewardDisplayService = rewardDisplayService;

            _model.OnPurchaseSuccess += HandlePurchaseSuccess;
            _model.OnPurchaseFailed += HandlePurchaseFailed;
            _model.OnPriceUpdated += HandlePriceUpdated;
            _model.OnRewardsGranted += HandleRewardsGranted;
            _model.OnPackResetExternally += HandlePackResetExternally;
        }

        public void RegisterView(IShopView view)
        {
            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
                view.Initialize(_model.GetShopItems(), this);
                view.SetBuyItemCallback(OnBuyItemRequested);
                foreach (var item in _model.GetShopItems())
                {
                    string price = _model.GetLocalizedPrice(item.productID);
                    if (!string.IsNullOrEmpty(price)) view.UpdatePrice(item.productID, price);
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

        public Sprite GetIconResourceReward(ResourceType resourceType, bool isInfinite)
        {
            if (_iconProvider == null) return null;
   
            return _iconProvider.GetRewardIcon(resourceType, isInfinite);
        }

        private void CleanUpDestroyedViews()
        {
            _activeViews.RemoveAll(view => 
                view == null || (view is MonoBehaviour mb && mb == null)
            );
        }

        private void OnBuyItemRequested(ProductID productId)
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews) view.ShowLoadingIndicator(true);
            _model.BuyItem(productId);
        }

        private void HandlePurchaseSuccess(ProductID productId, string message)
        {
            CleanUpDestroyedViews();
            
            foreach (var view in _activeViews)
            {
                view.ShowLoadingIndicator(false);
                view.OnPurchaseSuccess(productId);
            }
        }

        private void HandlePurchaseFailed(ProductID productId, string reason)
        {
            CleanUpDestroyedViews();
            Debug.LogError($"<color=red>[Shop UI]</color> Mua gói {productId} thất bại! Lý do: {reason}");
            foreach (var view in _activeViews)
            {
                view.ShowLoadingIndicator(false);
                view.OnPurchaseFailed(productId, reason);
            }
        }

        private void HandlePriceUpdated(ProductID productId, string price)
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews) view.UpdatePrice(productId, price);
        }

        private void HandleRewardsGranted(ShopItemData itemData, List<ShopItemReward> rewards)
        {
            CleanUpDestroyedViews();
            if (_eventService != null)
            {
                var eventData = new ShopNotificationEventData(itemData, rewards);
                _eventService.Publish(SharedEventType.OnShopRewardsNotificationRequested, eventData);
            }
        }
        private void HandlePackResetExternally(ProductID productId)
        {
            
            CleanUpDestroyedViews();
            RefreshShopItemsUI();
        }
        public bool IsItemOwned(ProductID productId)
        {
            var items = _model.GetShopItems();
    
            ShopItemData itemData = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].productID == productId)
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
                view.Initialize(_model.GetShopItems(), this);
            }
        }
        public void ResetPackAndRefresh(ProductID productId)
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