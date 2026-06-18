using System;

namespace ChieChie.Resource
{
    public interface IReadOnlyInfiniteStatus
    {
        bool IsCurrentlyInfinite(string resourceKey);
        TimeSpan GetRemainingInfiniteTime(string resourceKey);
    }
}
