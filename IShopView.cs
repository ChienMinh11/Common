using System;
using System.Collections.Generic;

namespace ChieChie.Constracts
{
    public interface IShopView
    {
        void Initialize(IReadOnlyList<IShopItemData> items);
        void SetBuyItemCallback(Action<string> callback);
        void UpdatePrice(string itemId, string price);
        void OnPurchaseSuccess(string itemId);
        void OnPurchaseFailed(string itemId, string reason);
        void ShowLoadingIndicator(bool show);
    }
}