using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "CORE/Configs/ResourceConfig")]
    public class ResourceConfig : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<ResourceData> resourcesList = new List<ResourceData>();
        private Dictionary<ResourceType, ResourceData> resourceMap = new Dictionary<ResourceType, ResourceData>();
        private readonly List<ResourceData> regenResourcesCache = new();

        public void OnAfterDeserialize()
        {
            resourceMap.Clear();
            regenResourcesCache.Clear();
            
            foreach (var resource in resourcesList)
            {
                if (resource != null && !resourceMap.ContainsKey(resource.key))
                {
                    resourceMap[resource.key] = resource;
                    
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
       
        public ResourceData GetResourceData(ResourceType type)
        {
            return resourceMap.TryGetValue(type, out var data) ? data : null;
        }
        public IReadOnlyList<ResourceData> GetAllRegenSettings() => regenResourcesCache;

        public IReadOnlyList<ResourceData> GetAllResources()
        {
            return resourcesList;
        }
    }
}