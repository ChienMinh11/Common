using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "CORE/Feature/ShopConfig")]
    public class ShopConfig : ScriptableObject
    {
        [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();
        public IReadOnlyList<ShopItemData> ShopItems => shopItems;
        
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (shopItems == null) return;

            for (int i = 0; i < shopItems.Count; i++)
            {
                var data = shopItems[i];
                if (data == null) continue;
                if (data.IdentitySource != null)
                {
                    if (!(data.IdentitySource is IShopProductIdentitySource))
                    {
                        Debug.LogError(
                            $"<color=red><b>[ResourceConfig LỖI]:</b></color> Shop Item [{i}] đang kéo file '{data.IdentitySource.name}' " +
                            $"KHÔNG triển khai interface IShopIdentitySource! Hệ thống đã tự động gỡ bỏ liên kết.");
                   
                        var field = typeof(ShopItemData).GetField("identitySource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        field?.SetValue(data, null);
                        continue;
                    }

                    if (data.Identity == null || string.IsNullOrEmpty(data.Identity.ProductId))
                    {
                       
                        Debug.LogWarning(
                            $"<color=yellow><b>[ResourceConfig CẢNH BÁO]:</b></color> Shop Item [{i}]] " +
                            $"đang dùng file '{data.IdentitySource.name}' nhưng file này đang BỎ TRỐNG trường ResourceId! " +
                            $"Vui lòng nhập ID tĩnh cho asset này để đảm bảo an toàn Save/Load.");
                    }
                }
             
                for (int j = 0; j < data.rewards.Count; j++)
                {
                    var resData = data.rewards[j] as ShopItemReward; 
                    if (resData == null) continue;
                    
                    if (resData.IdentitySource != null)
                    {
                        if (!(resData.IdentitySource is IShopIdentitySource))
                        {
                            Debug.LogError(
                                $"<color=red><b>[ResourceConfig LỖI]:</b></color> Shop Item [{i}], Reward [{j}] đang kéo file '{resData.IdentitySource.name}' " +
                                $"KHÔNG triển khai interface IShopIdentitySource! Hệ thống đã tự động gỡ bỏ liên kết.");
                   
                            var field = typeof(ShopItemReward).GetField("identitySource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            field?.SetValue(resData, null);
                            continue;
                        }

                        if (resData.Identity == null || string.IsNullOrEmpty(resData.Identity.ResourceId))
                        {
                            // FIX: Sửa log hiển thị index i và j
                            Debug.LogWarning(
                                $"<color=yellow><b>[ResourceConfig CẢNH BÁO]:</b></color> Shop Item [{i}], Reward [{j}] " +
                                $"đang dùng file '{resData.IdentitySource.name}' nhưng file này đang BỎ TRỐNG trường ResourceId! " +
                                $"Vui lòng nhập ID tĩnh cho asset này để đảm bảo an toàn Save/Load.");
                        }
                    }
                }
            }
        }
#endif
    }

    [Serializable]
    public class ShopItemData: IShopItemData
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
        public List<ShopItemReward> rewards = new List<ShopItemReward>();
        public bool isOneTimePurchase;
        public bool isTimeLimited;
        
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool IsOneTimePurchase => isOneTimePurchase;
        public bool IsTimeLimited => isTimeLimited;

        public List<IShopItemReward> Rewards => rewards.ConvertAll(x => (IShopItemReward)x);

    }

    [Serializable]
    public class ShopItemReward:IShopItemReward
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