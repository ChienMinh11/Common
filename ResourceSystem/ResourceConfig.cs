using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Runtime
{
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "MyFramework/Config/ResourceConfig")]
    public class ResourceConfig : ScriptableObject
    {
        [SerializeField] private List<ResourceData> resources = new List<ResourceData>();
        
        private Dictionary<ResourceType, ResourceData> resourceMap;

        public void Initialize()
        {
            resourceMap = new Dictionary<ResourceType, ResourceData>();
            foreach (var resource in resources)
            {
                resourceMap[resource.key] = resource;
            }
        }

        public ResourceData GetResourceData(ResourceType type)
        {
            return resourceMap.TryGetValue(type, out var data) ? data : null;
        }

        public List<ResourceData> GetAllResources()
        {
            return resources;
        }
    }
    
}
