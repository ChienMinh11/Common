using System;
using UnityEngine;

namespace ChieChie.Core
{
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
