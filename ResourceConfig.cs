using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Resource
{
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "CORE/Configs/ResourceConfig")]
    public class ResourceConfig : ScriptableObject
    {
        [SerializeField] private List<ResourceData> resourcesList = new List<ResourceData>();
        
        private readonly Dictionary<string, ResourceData> resourceMap = new Dictionary<string, ResourceData>();
        private readonly List<ResourceData> regenResourcesCache = new List<ResourceData>();
        private bool isInitialized = false;

        private void EnsureInitialized()
        {
            if (isInitialized && resourceMap.Count > 0) return;

            resourceMap.Clear();
            regenResourcesCache.Clear();

            foreach (var resource in resourcesList)
            {
                if (resource != null)
                {
                    string id = resource.ResourceId;
                    if (!string.IsNullOrEmpty(id) && !resourceMap.ContainsKey(id))
                    {
                        resourceMap[id] = resource;
                        if (resource.HasRegen)
                        {
                            regenResourcesCache.Add(resource);
                        }
                    }
                }
            }
            isInitialized = true;
        }
       
        public ResourceData GetResourceData(string resourceKey)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(resourceKey)) return null;
            return resourceMap.TryGetValue(resourceKey, out var data) ? data : null;
        }

        public IReadOnlyList<ResourceData> GetAllRegenSettings()
        {
            EnsureInitialized(); 
            return regenResourcesCache;
        }

        public IReadOnlyList<ResourceData> GetAllResources()
        {
            return resourcesList;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            isInitialized = false; // Reset trạng thái để nạp lại khi có thay đổi trong Inspector
            if (resourcesList == null) return;

            for (int i = 0; i < resourcesList.Count; i++)
            {
                var resData = resourcesList[i];
                if (resData == null) continue;

                if (resData.IdentitySource != null)
                {
                    if (!(resData.IdentitySource is IResourceIdentitySource))
                    {
                        Debug.LogError(
                            $"<color=red><b>[ResourceConfig LỖI]:</b></color> Phần tử thứ [{i}] đang kéo file '{resData.IdentitySource.name}' " +
                            $"KHÔNG triển khai interface IResourceIdentitySource! Hệ thống đã tự động gỡ bỏ liên kết.");
                   
                        var field = typeof(ResourceData).GetField("identitySource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        field?.SetValue(resData, null);
                        continue;
                    }

                    if (resData.Identity == null || string.IsNullOrEmpty(resData.Identity.ResourceId))
                    {
                        Debug.LogWarning(
                            $"<color=yellow><b>[ResourceConfig CẢNH BÁO]:</b></color> Phần tử thứ [{i}] " +
                            $"đang dùng file '{resData.IdentitySource.name}' nhưng file này đang BỎ TRỐNG trường ResourceId! " +
                            $"Vui lòng nhập ID tĩnh cho asset này để đảm bảo an toàn Save/Load.");
                    }
                }
            }
        }
#endif
    }
}