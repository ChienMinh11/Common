using System;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class ResourceRegenData
    {
        public ResourceType resourceType;
        public bool isRegenEnabled;
        public int regenIntervalMinutes = 30; // Mặc định 30 phút
        public long regenAmountPerInterval = 1; // Mỗi lần hồi 1 điểm
    }
}
