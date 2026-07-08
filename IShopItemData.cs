using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IShopItemData
    {
        string ProductID {get;}
        string DisplayName { get; }
        string Description { get; }
        Sprite Icon { get; }
        bool IsOneTimePurchase{ get; }
        bool IsTimeLimited { get; }
        List<IItemReward> Rewards { get; }
        List<string> ExclusiveProductIds { get; }
        string PopupName { get; }
    }
}
