using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class ShopItemData
    {
        public ProductID productID;
        public string displayName;
        public string description;
        public Sprite icon;
        public List<ShopItemReward> rewards = new List<ShopItemReward>();
        public bool isOneTimePurchase;
        public bool isTimeLimited;
    }
}
