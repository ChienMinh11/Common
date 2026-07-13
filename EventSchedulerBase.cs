using System;
using ChieChie.Constracts;

namespace ChieChie.GenericLiveOps
{
    [Serializable]
    public abstract class EventSchedulerBase : IEventScheduler
    {
        public string eventId;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;

        public string EventId => eventId;
        public DateTime StartTime => startTime;
        public DateTime EndTime => endTime;
        public bool IsActive => isActive;

        public void UpdateSchedule(ITimeProvider timeProvider)
        {
            var now = timeProvider.UtcNow;
            if (!TryCreateCurrentEvent(now, out eventId, out startTime, out endTime))
            {
                Clear();
                return;
            }

            UpdateActiveState(now);
        }

        public bool SyncFromEventId(string savedEventId, ITimeProvider timeProvider)
        {
            eventId = savedEventId ?? string.Empty;

            if (!TryGetEventWindow(eventId, out var parsedStartTime, out var parsedEndTime))
            {
                startTime = DateTime.MinValue;
                endTime = DateTime.MinValue;
                isActive = false;
                return false;
            }

            startTime = parsedStartTime;
            endTime = parsedEndTime;
            UpdateActiveState(timeProvider.UtcNow);
            return true;
        }

        public abstract bool TryGetEventWindow(string candidateEventId, out DateTime parsedStartTime, out DateTime parsedEndTime);

        public void Clear()
        {
            eventId = string.Empty;
            isActive = false;
            startTime = DateTime.MinValue;
            endTime = DateTime.MinValue;
        }

        public TimeSpan GetRemainingTime(ITimeProvider timeProvider)
        {
            var remaining = endTime - timeProvider.UtcNow;
            return remaining.TotalSeconds < 1.0 ? TimeSpan.Zero : remaining;
        }

        public bool IsExpired(ITimeProvider timeProvider)
        {
            return timeProvider.UtcNow >= endTime;
        }

        protected abstract bool TryCreateCurrentEvent(
            DateTime now,
            out string currentEventId,
            out DateTime currentStartTime,
            out DateTime currentEndTime);

        private void UpdateActiveState(DateTime now)
        {
            isActive = now >= startTime && now <= endTime;
        }
    }
}
