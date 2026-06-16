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
    public class ShopItemReward : BaseRewardData
    {
        public RewardType rewardType = RewardType.Resource;
        
        // Giữ lại cho backward compatibility
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
    public enum RewardType
    {
        Resource = 0,   
        NoAds = 1,
        GoldenPass = 2,
  
    }
 
}
