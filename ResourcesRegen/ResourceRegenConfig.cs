using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    [CreateAssetMenu(fileName = "ResourceRegenConfig", menuName = "CORE/Configs/ResourceRegenConfig")]
    public class ResourceRegenConfig : ScriptableObject
    {
        [SerializeField] private List<ResourceRegenData> regenSettings = new();
        private Dictionary<ResourceType, ResourceRegenData> _regenMap;

        public void Initialize()
        {
            _regenMap = new Dictionary<ResourceType, ResourceRegenData>();
            foreach (var setting in regenSettings)
            {
                _regenMap[setting.resourceType] = setting;
            }
        }

        public ResourceRegenData GetRegenData(ResourceType type)
        {
            return _regenMap.TryGetValue(type, out var data) ? data : null;
        }

        public List<ResourceRegenData> GetAllRegenSettings() => regenSettings;
    }
}
