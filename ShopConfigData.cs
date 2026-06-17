using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Shop
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "CORE/Feature/ShopConfig")]
    public class ShopConfig : ScriptableObject
    {
       [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();
        public IReadOnlyList<ShopItemData> ShopItems => shopItems;
    }
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
    [Serializable]
    public class ShopItemReward
    {
        public int resourceType; 
        public long amount;
        public bool isInfiniteReward;
        public float infinityDuration;

        public Sprite IconReward;
        public Sprite InfinityRewardIcon;
        
        public bool isInfinite
        {
            get => isInfiniteReward;
            set => isInfiniteReward = value;
        }
        
        public float infiniteDuration
        {
            get => infinityDuration;
            set => infinityDuration = value;
        }
    }
    
 
}
