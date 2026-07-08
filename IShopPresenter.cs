using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IShopPresenter
    {
        void RegisterView(IShopView view);
        void UnregisterView(IShopView view);
        Sprite GetIconResourceReward(string resourceType, bool isInfinite);
        bool IsItemOwned(string productId);
    }
}
