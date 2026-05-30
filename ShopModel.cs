using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    public class ShopModel
    {
        private readonly IIapService _iapService;
        private readonly ShopConfig _shopConfig;
        private readonly ISaveSystem _saveSystem;
        private readonly ShopStorage _shopStorage;
        
    
        public ShopModel(IIapService iapService, SaveSystem saveSystem, ShopConfig shopConfig)
        {
           _iapService= iapService;
            _saveSystem = saveSystem;
            _shopConfig = shopConfig;
            _shopStorage = new ShopStorage(saveSystem);
        }
    }
}