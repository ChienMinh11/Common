using System;
using ChieChie.Core;

namespace ChieChie.Resource
{
    public interface IReadOnlyInfiniteStatus
    {
        bool IsCurrentlyInfinite(ResourceType resourceType);
        TimeSpan GetRemainingInfiniteTime(ResourceType resourceType);
    }
}
