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
        
        public event Action<string> OnBuySuccess;
        public event Action<List<ResourceRewardCommand>> OnRequestAddResource;
        public event Action<IShopNotificationEventData> OnShopRewardsNotificationRequested;
       
        public bool IsInitialized { get; set; }

        public ShopManager(ShopConfig shopConfig, IShopIapBrigde iapBridge, IShopSaveAdapter saveAdapter)
        {
            _config = shopConfig;
            _iapBridge = iapBridge;
            _saveAdapter = saveAdapter; 
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _shopModel = new ShopModel(_iapBridge, _saveAdapter, _config);
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

        public bool IsItemOwned(string productId) => Presenter.IsItemOwned(productId);

        public void ResetPackTimeLimited(string id) => Presenter?.ResetPackAndRefresh(id);
       
    }
}