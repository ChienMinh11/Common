using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    [CreateAssetMenu(fileName = "ResourceRegenConfig", menuName = "CORE/Configs/ResourceRegenConfig")]
    public class ResourceRegenConfig : ScriptableObject
    {
        [SerializeField] private System.Collections.Generic.List<ResourceRegenData> regenSettings = new();
        private System.Collections.Generic.Dictionary<ResourceType, ResourceRegenData> _map;

        public void Initialize()
        {
            _map = new();
            foreach (var setting in regenSettings)
            {
                _map[setting.resourceType] = setting;
            }
        }

        public ResourceRegenData GetRegenData(ResourceType type)
        {
            return _map != null && _map.TryGetValue(type, out var data) ? data : null;
        }
    }
}
