using System;
using System.Collections.Generic;

namespace ChieChie.Core
{
    public interface IShopView
    {
        void Initialize(List<ShopItemData> items,ShopPresenter shopPresenter);
        void SetBuyItemCallback(Action<ProductID> callback);
        void UpdatePrice(ProductID itemId, string price);
        void OnPurchaseSuccess(ProductID itemId);
        void OnPurchaseFailed(ProductID itemId, string reason);
        void ShowLoadingIndicator(bool show);
        void ShowRewardsNotification(List<ShopItemReward> rewards);
    }
}