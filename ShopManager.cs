using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;

namespace ChieChie.Shop
{
    public class ShopManager : IDisposable,IShopService
    {
        private readonly ShopConfig _config;
        public ShopPresenter Presenter { get; private set; }

        private ShopModel _shopModel;
        private readonly IShopIapBrigde _iapBridge;
        private readonly IShopSaveAdapter _saveAdapter;
        private readonly RewardDisplayService _rewardDisplayService;
        
        public event Action<ProductID> OnBuySuccess;
        public event Action<List<ResourceRewardCommand>> OnRequestAddResource;
        public event Action<ShopNotificationEventData> OnShopRewardsNotificationRequested;
       
        public bool IsInitialized { get; set; }

        public ShopManager(ShopConfig shopConfig, IShopIapBrigde iapBridge, IShopSaveAdapter saveAdapter, 
           RewardDisplayService rewardDisplayService)
        {
            _config = shopConfig;
            _iapBridge = iapBridge;
            _saveAdapter = saveAdapter; 
            _rewardDisplayService = rewardDisplayService; 
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _shopModel = new ShopModel(_iapBridge,_saveAdapter, _config);
            Presenter = new ShopPresenter(_shopModel,_rewardDisplayService);
            
            _shopModel.OnBuySuccessExternal += HandleBuySuccessExternal;
            _shopModel.OnRequestAddResource += HandleRequestAddResourceExternal;
            Presenter.OnShopRewardsNotificationRequested += HandleNotificationRequestedExternal;
            
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        private void HandleBuySuccessExternal(ProductID id) => OnBuySuccess?.Invoke(id);
        private void HandleRequestAddResourceExternal(List<ResourceRewardCommand> cmds) => OnRequestAddResource?.Invoke(cmds);
        private void HandleNotificationRequestedExternal(ShopNotificationEventData data) => OnShopRewardsNotificationRequested?.Invoke(data);

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

        public void ResetPackTimeLimited(ProductID id)
        {
            Presenter?.ResetPackAndRefresh(id);
        }
    }
}