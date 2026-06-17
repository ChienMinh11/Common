using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Resource
{
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "CORE/Configs/ResourceConfig")]
    public class ResourceConfig : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<ResourceData> resourcesList = new List<ResourceData>();
        
        // Không dùng Dictionary làm trường lưu trữ trực tiếp nếu không thể bảo đảm tiến trình Deserialize
        private readonly Dictionary<int, ResourceData> resourceMap = new Dictionary<int, ResourceData>();
        private readonly List<ResourceData> regenResourcesCache = new List<ResourceData>();
        private bool isInitialized = false;

        public void OnAfterDeserialize()
        {
            isInitialized = false;
        }

        public void OnBeforeSerialize() { }

        private void EnsureInitialized()
        {
            if (isInitialized && resourceMap.Count > 0) return;

            resourceMap.Clear();
            regenResourcesCache.Clear();

            foreach (var resource in resourcesList)
            {
                if (resource != null)
                {
                    int hash = resource.HashId;
                    if (!resourceMap.ContainsKey(hash))
                    {
                        resourceMap[hash] = resource;
                        if (resource.HasRegen)
                        {
                            regenResourcesCache.Add(resource);
                        }
                    }
                }
            }
            isInitialized = true;
        }
       
        public ResourceData GetResourceData(int typeHash)
        {
            EnsureInitialized();
            return resourceMap.TryGetValue(typeHash, out var data) ? data : null;
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
    }
}