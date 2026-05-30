using System;
using UnityEngine;

namespace ChieChie.Core
{
    public enum ProductID 
    {
        NONE = 0,
        
        REMOVE_ADS = 1,
    
        COINS_PACK_1 = 2,
        COINS_PACK_2 = 3,
        COINS_PACK_3 = 4,
        COINS_PACK_4 = 5,
        COINS_PACK_5 = 6,
        COINS_PACK_6 = 7,
        
        STARTER_PACK = 8,
        POWERUPS_PACK = 9,
        PRO_PACK = 10,
        MASTER_PACK = 11,
        MEGA_PACK = 12,
        ULTRA_PACK = 13,
        GIANT_PACK = 14,
        LEGENDARY_PACK = 15,
        LIVE_PACK = 16,
        REVIVAL_PACK = 17,
        
        GOLDEN_PASS = 50,
    }
    public interface IIapBrigde
    {
        bool IsInitialized { get; }
        string GetLocalizedPrice(ProductID productId);
        void BuyProduct(ProductID productID, Action onSuccess = null, Action<string> onFailure = null);
        void RestorePurchases();
        event Action<ProductID> OnPurchaseSuccess;
        event Action<ProductID, string> OnPurchaseFailure;
        event Action<ProductID, string> OnPriceUpdated;
        event Action<bool, string> OnRestorePurchasesCompleted;
    }
}
