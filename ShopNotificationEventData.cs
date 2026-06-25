using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopNotificationEventData:IShopNotificationEventData
    {
        public IShopItemData ItemData { get; }
        public System.Collections.Generic.List<IShopItemReward> Rewards { get; }

        public ShopNotificationEventData(IShopItemData itemData, System.Collections.Generic.List<IShopItemReward> rewards)
        {
            ItemData = itemData;
            Rewards = rewards;
        }
    }
}
