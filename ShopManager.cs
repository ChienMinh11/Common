using System;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ChieChie.Shop
{
    public class ShopManager : IDisposable,IShopService
    {
        private readonly ShopConfig _config;
        public ShopPresenter Presenter { get; private set; }

        private ShopModel _shopModel;
        private readonly IShopIapBrigde _iapBridge;
        private readonly ISaveSystem _saveSystem;
        private readonly IEventService _eventService;
        private readonly IIconProvider _iconProvider;
        private readonly RewardDisplayService _rewardDisplayService;

        public bool IsInitialized { get; set; }

        public ShopManager(ShopConfig shopConfig,IShopIapBrigde iapBridge, ISaveSystem saveSystem, IEventService eventService, 
            IIconProvider iconProvider, RewardDisplayService rewardDisplayService)
        {
            _config = shopConfig;
            _iapBridge = iapBridge;
            _saveSystem = saveSystem;
            _eventService = eventService;
            _iconProvider = iconProvider;
            _rewardDisplayService = rewardDisplayService; 
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _shopModel = new ShopModel(_iapBridge, _saveSystem, _config, _eventService);
            Presenter = new ShopPresenter(_shopModel, _iconProvider, _eventService, _rewardDisplayService);
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

       

        public void Dispose()
        {
            Presenter?.Dispose();
        }

        public void ResetPackTimeLimited(ProductID id)
        {
            Presenter?.ResetPackAndRefresh(id);
        }
    }
}