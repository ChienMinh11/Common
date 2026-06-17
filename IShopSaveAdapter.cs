using UnityEngine;

namespace ChieChie.Shop
{
    public interface IShopSaveAdapter
    {
        string LoadOneTimePurchases();
        void SaveOneTimePurchases(string data);
        string LoadTimeLimitedPurchases();
        void SaveTimeLimitedPurchases(string data);
    }
}
