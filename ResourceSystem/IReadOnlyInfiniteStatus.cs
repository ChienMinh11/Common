using System;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IReadOnlyInfiniteStatus
    {
        bool IsCurrentlyInfinite(ResourceType resourceType);
        TimeSpan GetRemainingInfiniteTime(ResourceType resourceType);
    }
}
