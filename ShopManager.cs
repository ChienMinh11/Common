using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopManager : IDisposable, IShopService
    {
        private readonly ShopConfig _config;
        public ShopPresenter Presenter { get; private set; }

        private ShopModel _shopModel;
        private readonly IShopIapBrigde _iapBridge;
        private readonly IShopSaveAdapter _saveAdapter;
        private readonly IShopTimeProvider _timeProvider;
        
        public event Action<string> OnBuySuccess;
        public event Action<List<ResourceRewardCommand>> OnRequestAddResource;
        public event Action<IShopNotificationEventData> OnShopRewardsNotificationRequested;
       
        public bool IsInitialized { get; set; }

        public ShopManager(ShopConfig shopConfig, IShopIapBrigde iapBridge, IShopSaveAdapter saveAdapter, IShopTimeProvider timeProvider = null)
        {
            _config = shopConfig;
            _iapBridge = iapBridge;
            _saveAdapter = saveAdapter;
            _timeProvider = timeProvider;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            var offerAvailabilityService = new ShopOfferAvailabilityService(_timeProvider);
            _shopModel = new ShopModel(_iapBridge, _saveAdapter, _config, offerAvailabilityService);
            Presenter = new ShopPresenter(_shopModel);
            
            _shopModel.OnBuySuccessExternal += HandleBuySuccessExternal;
            _shopModel.OnRequestAddResource += HandleRequestAddResourceExternal;
            Presenter.OnShopRewardsNotificationRequested += HandleNotificationRequestedExternal;
            
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        private void HandleBuySuccessExternal(string id) => OnBuySuccess?.Invoke(id);
        private void HandleRequestAddResourceExternal(List<ResourceRewardCommand> cmds) => OnRequestAddResource?.Invoke(cmds);
        private void HandleNotificationRequestedExternal(IShopNotificationEventData data) => OnShopRewardsNotificationRequested?.Invoke(data);

        public void Dispose()
        {
            if (_shopModel != null)
            {
                _shopModel.OnBuySuccessExternal -= HandleBuySuccessExternal;
                _shopModel.OnRequestAddResource -= HandleRequestAddResourceExternal;
            }
            if (Presenter != null)
            {
                Presenter.OnShopRewardsNotificationRequested -= HandleNotificationRequestedExternal;
            }

            Presenter?.Dispose();
        }
      

        public void RegisterView(IShopView view) => Presenter?.RegisterView(view);

        public void UnregisterView(IShopView view) => Presenter?.UnregisterView(view);

        public Sprite GetIconResourceReward(string resourceType, bool isInfinite) => Presenter?.GetIconResourceReward(resourceType, isInfinite);

        public bool IsItemOwned(string productId) => Presenter != null && Presenter.IsItemOwned(productId);

        public bool IsOfferAvailable(string productId) => Presenter != null && Presenter.IsOfferAvailable(productId);

        public bool TryGetOfferTimeRemaining(string productId, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            return Presenter != null && Presenter.TryGetOfferTimeRemaining(productId, out remaining);
        }

        public void RefreshShopItems() => Presenter?.RefreshShopItemsUI();

        public void ResetPackTimeLimited(string id) => Presenter?.ResetPackAndRefresh(id);
       
    }
}
