using UnityEngine;

namespace ChieChie.Core
{
    public interface IReadOnlyInfiniteStatus
    {
        bool IsCurrentlyInfinite(ResourceType resourceType);
    }
}
