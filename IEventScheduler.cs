using System;
using ChieChie.Constracts;

namespace ChieChie.GenericLiveOps
{
    public interface IEventScheduler
    {
        string EventId { get; }
        DateTime StartTime { get; }
        DateTime EndTime { get; }
        bool IsActive { get; }

        void UpdateSchedule(ITimeProvider timeProvider);
        bool SyncFromEventId(string savedEventId, ITimeProvider timeProvider);
        bool TryGetEventWindow(string candidateEventId, out DateTime parsedStartTime, out DateTime parsedEndTime);
        void Clear();
        TimeSpan GetRemainingTime(ITimeProvider timeProvider);
        bool IsExpired(ITimeProvider timeProvider);
    }
}
