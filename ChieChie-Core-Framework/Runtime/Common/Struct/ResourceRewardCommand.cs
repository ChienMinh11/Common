using UnityEngine;

namespace ChieChie.Core
{
    public struct ResourceRewardCommand
    {
        public ResourceType ResourceType;
        public long Amount;
        public bool IsInfinite;
        public float DurationInSeconds;
    }
}
