using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    [CreateAssetMenu(fileName = "NewShopItem", menuName = "CORE/Feature/ShopItem")]
    public class ShopItemData:ScriptableObject,IShopItemData
    {
        [Header("Identity Reference")]
        [Tooltip("Kéo thả ScriptableObject (có implement IShopProductIdentitySource) vào đây")]
        [SerializeField] private UnityEngine.Object identitySource; 
        public UnityEngine.Object IdentitySource => identitySource;
        public IShopProductIdentitySource Identity => identitySource as IShopProductIdentitySource;
        
        public string ProductID
        {
            get 
            {
                if (Identity != null && !string.IsNullOrEmpty(Identity.ProductId))
                {
                    return Identity.ProductId;
                }
                return string.Empty; 
            }
        }

       
        public string displayName;
        public string description;
        public Sprite icon;
        public List<ItemReward> rewards = new List<ItemReward>();
        public bool isOneTimePurchase;
        public bool isTimeLimited;
        [Header("Exclusive Settings")]
        [Tooltip("Khi mua gói này, các gói có ProductID trong danh sách này cũng sẽ được ẩn đi / coi như đã sở hữu")]
        [SerializeField] private List<UnityEngine.Object> exclusiveProductSources = new List<UnityEngine.Object>();
        public List<UnityEngine.Object> ExclusiveProductSources => exclusiveProductSources;

        public List<string> ExclusiveProductIds
        {
            get
            {
                var ids = new List<string>();
                if (exclusiveProductSources == null) return ids;

                foreach (var source in exclusiveProductSources)
                {
                    if (source is IShopProductIdentitySource identity)
                    {
                        if (!string.IsNullOrEmpty(identity.ProductId))
                        {
                            ids.Add(identity.ProductId);
                        }
                    }
                }
                return ids;
            }
        }

        public string PopupName
        {
            get 
            {
                if (Identity != null && !string.IsNullOrEmpty(Identity.PopupName))
                {
                    return Identity.PopupName;
                }
                return string.Empty; 
            }
        }

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool IsOneTimePurchase => isOneTimePurchase;
        public bool IsTimeLimited => isTimeLimited;

        public List<IItemReward> Rewards => rewards.ConvertAll(x => (IItemReward)x);

    }

    [Serializable]
    public class ItemReward:IItemReward
    {
        [Header("Identity Reference")]
        [Tooltip("Kéo thả ScriptableObject (có implement IShopIdentitySource) vào đây")]
        [SerializeField] private UnityEngine.Object identitySource; 
        public UnityEngine.Object IdentitySource => identitySource;
        public IShopIdentitySource Identity => identitySource as IShopIdentitySource;

        public string ResourceId
        {
            get 
            {
                if (Identity != null && !string.IsNullOrEmpty(Identity.ResourceId))
                {
                    return Identity.ResourceId;
                }
                return string.Empty; 
            }
        }

        public long Amount => amount;
        public bool IsInfiniteReward => isInfiniteReward;
        public float InfinityDuration => infinityDuration;


        public long amount;
        public bool isInfiniteReward;
        public float infinityDuration;

        public Sprite IconReward
        {
            get
            {
                if (Identity != null && Identity.Icon != null)
                {
                    return Identity.Icon;
                }
                return null; 
            }
        }
      

        public Sprite InfinityRewardIcon  {
            get
            {
                if (Identity != null && Identity.InfinityIcon != null)
                {
                    return Identity.InfinityIcon;
                }
                return null; 
            }
        }
     

    }
}
