using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    public class ShopPresenter : IDisposable
    {
        private readonly ShopModel _model;
        private readonly List<IShopView> _activeViews = new List<IShopView>();
        private ResourceConfig _resourceConfig;

        public ShopPresenter(ShopModel model,ResourceConfig resourceConfig)
        {
            _model = model;
            _resourceConfig = resourceConfig;
            _model.OnPurchaseSuccess += HandlePurchaseSuccess;
            _model.OnPurchaseFailed += HandlePurchaseFailed;
            _model.OnPriceUpdated += HandlePriceUpdated;
            _model.OnRewardsGranted += HandleRewardsGranted;
        }

        public void RegisterView(IShopView view)
        {
            CleanUpDestroyedViews(); // Dọn dẹp trước khi thêm mới
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
                view.Initialize(_model.GetShopItems(), this);
                view.SetBuyItemCallback(OnBuyItemRequested);
                
                // Đồng bộ giá ngay khi View vừa xuất hiện
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
            if (_resourceConfig == null) return null;
            ResourceData data = _resourceConfig.GetResourceData(resourceType);
            if (data == null) return null;
            return isInfinite ? (data.infinityIcon != null ? data.infinityIcon : data.icon) : data.icon;
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

        private void HandleRewardsGranted(List<ShopItemReward> rewards)
        {
            CleanUpDestroyedViews();
            foreach (var view in _activeViews) view.ShowRewardsNotification(rewards);
        }

        public void Dispose()
        {
            _model.OnPurchaseSuccess -= HandlePurchaseSuccess;
            _model.OnPurchaseFailed -= HandlePurchaseFailed;
            _model.OnPriceUpdated -= HandlePriceUpdated;
            _model.OnRewardsGranted -= HandleRewardsGranted;
            _activeViews.Clear();
            _model.Dispose();
        }
    }
}