using System;

namespace ChieChie.Resource
{
    public interface IReadOnlyInfiniteStatus
    {
        bool IsCurrentlyInfinite(int resourceHash);
        TimeSpan GetRemainingInfiniteTime(int resourceHash);
    }
}
