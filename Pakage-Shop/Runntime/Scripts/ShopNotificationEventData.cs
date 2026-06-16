using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopNotificationEventData
    {
        public ShopItemData ItemData { get; }
        public System.Collections.Generic.List<ShopItemReward> Rewards { get; }

        public ShopNotificationEventData(ShopItemData itemData, System.Collections.Generic.List<ShopItemReward> rewards)
        {
            ItemData = itemData;
            Rewards = rewards;
        }
    }
}
