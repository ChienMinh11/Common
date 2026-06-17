using System;
using ChieChie.Core;

namespace ChieChie.Shop
{
    public interface IShopIapBrigde
    {
        string GetLocalizedPrice(ProductID productId);
        void BuyProduct(ProductID productID, Action onSuccess = null, Action<string> onFailure = null);
        void RestorePurchases();
        event Action<ProductID> OnPurchaseSuccess;
        event Action<ProductID, string> OnPurchaseFailure;
        event Action<ProductID, string> OnPriceUpdated;
        event Action<bool, string> OnRestorePurchasesCompleted;
    }
}
