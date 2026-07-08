using System;

namespace ChieChie.Shop
{
    public interface IShopIapBrigde
    {
        string GetLocalizedPrice(string productId);
        void BuyProduct(string productId, Action onSuccess = null, Action<string> onFailure = null);
        void RestorePurchases();
        event Action<string> OnPurchaseSuccess;
        event Action<string, string> OnPurchaseFailure;
        event Action<string, string> OnPriceUpdated;
        event Action<bool, string> OnRestorePurchasesCompleted;
    }
}
