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
                if (data.ExclusiveProductSources != null)
                {
                    for (int k = data.ExclusiveProductSources.Count - 1; k >= 0; k--)
                    {
                        var src = data.ExclusiveProductSources[k];
                        if (src == null) continue;

                        if (!(src is IShopProductIdentitySource))
                        {
                            Debug.LogError(
                                $"<color=red><b>[ShopConfig LỖI]:</b></color> Shop Item [{i}] (gói {data.displayName}) tại Exclusive List đang kéo file '{src.name}' " +
                                $"KHÔNG triển khai interface IShopProductIdentitySource! Hệ thống đã tự động gỡ bỏ.");
                            
                            data.ExclusiveProductSources.RemoveAt(k);
                        }
                    }
                }
             
                for (int j = 0; j < data.rewards.Count; j++)
                {
                    var resData = data.rewards[j] as ItemReward; 
                    if (resData == null) continue;
                    
                    if (resData.IdentitySource != null)
                    {
                        if (!(resData.IdentitySource is IShopIdentitySource))
                        {
                            Debug.LogError(
                                $"<color=red><b>[ResourceConfig LỖI]:</b></color> Shop Item [{i}], Reward [{j}] đang kéo file '{resData.IdentitySource.name}' " +
                                $"KHÔNG triển khai interface IShopIdentitySource! Hệ thống đã tự động gỡ bỏ liên kết.");
                   
                            var field = typeof(ItemReward).GetField("identitySource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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

   
}