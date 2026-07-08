using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IShopNotificationEventData 
    {
        IShopItemData ItemData { get; }
        System.Collections.Generic.List<IItemReward> Rewards { get; }
    }
}
