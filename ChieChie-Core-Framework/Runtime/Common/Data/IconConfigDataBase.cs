using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class IconData
    {
        public ResourceType resourceType;
        public Sprite normalIcon;
        public Sprite infiniteIcon; 
    }

    [CreateAssetMenu(fileName = "IconConfigSO", menuName = "CORE/Configs/IconConfigSO")]
    public class IconConfigDataBase : ScriptableObject, IIconProvider
    {
        [SerializeField] private List<IconData> iconDatabase;

        private Dictionary<ResourceType, IconData> _cache;

        public void Initialize()
        {
            _cache = new Dictionary<ResourceType, IconData>();
            foreach (var data in iconDatabase)
            {
                if (!_cache.ContainsKey(data.resourceType))
                    _cache.Add(data.resourceType, data);
            }
        }

        public Sprite GetIcon(ResourceType type, bool isInfinite = false)
        {
            if (_cache == null) Initialize(); // Fail-safe nếu chưa init

            if (_cache.TryGetValue(type, out var data))
            {
                return isInfinite && data.infiniteIcon != null ? data.infiniteIcon : data.normalIcon;
            }

            return null;
        }

        public Sprite GetRewardIcon(ResourceType type, bool isInfinite)
        {
            return GetIcon(type, isInfinite);
        }
    }
}
