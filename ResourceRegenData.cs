using System;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class ResourceRegenData
    {
        public ResourceType resourceType;
        public long regenAmount = 1;        // Số lượng hồi phục mỗi chu kỳ
        public float intervalSeconds = 1800f;   // Chu kỳ hồi phục (giây)
        public bool isEnabledByDefault = true;
    }
}
