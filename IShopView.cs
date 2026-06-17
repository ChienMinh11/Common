using System;
using System.Collections.Generic;
using ChieChie.Core;
using Cysharp.Threading.Tasks;

namespace ChieChie.Shop
{
    public interface IShopView
    {
        void Initialize(IReadOnlyList<ShopItemData> items, ShopPresenter shopPresenter);
        void SetBuyItemCallback(Action<ProductID> callback);
        void UpdatePrice(ProductID itemId, string price);
        void OnPurchaseSuccess(ProductID itemId);
        void OnPurchaseFailed(ProductID itemId, string reason);
        void ShowLoadingIndicator(bool show);
    }
}