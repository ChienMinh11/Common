using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "CORE/Configs/ResourceConfig")]
    public class ResourceConfig : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<ResourceData> resourcesList = new List<ResourceData>();
        private Dictionary<int, ResourceData> resourceMap = new Dictionary<int, ResourceData>();
        private readonly List<ResourceData> regenResourcesCache = new();

        public void OnAfterDeserialize()
        {
            resourceMap.Clear();
            regenResourcesCache.Clear();
            
            foreach (var resource in resourcesList)
            {
                if (resource != null && !resourceMap.ContainsKey(resource.HashId))
                {
                    resourceMap[resource.HashId] = resource;
                    
                    if (resource.HasRegen)
                    {
                        regenResourcesCache.Add(resource);
                    }
                }
            }
        }

        public void OnBeforeSerialize()
        {
            // Thường để trống trừ khi bạn muốn đồng bộ ngược từ Dict vào List trong Runtime
        }
       
        public ResourceData GetResourceData(int typeHash)
        {
            return resourceMap.TryGetValue(typeHash, out var data) ? data : null;
        }
        public IReadOnlyList<ResourceData> GetAllRegenSettings() => regenResourcesCache;

        public IReadOnlyList<ResourceData> GetAllResources()
        {
            return resourcesList;
        }
    }
}