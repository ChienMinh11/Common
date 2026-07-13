using System;

namespace ChieChie.Constracts
{
    [Serializable]
    public class MonthlyEventScheduler
    {
        private readonly string _eventIdPrefix;

        public string eventId;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;

        public MonthlyEventScheduler(string eventIdPrefix)
        {
            _eventIdPrefix = eventIdPrefix;
            eventId = string.Empty;
        }

        public void UpdateMonthlySchedule(ITimeProvider timeProvider)
        {
            var now = timeProvider.UtcNow;
            startTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            endTime = startTime.AddMonths(1).AddSeconds(-1);
            eventId = $"{_eventIdPrefix}_{startTime:yyyyMM}";
            isActive = now >= startTime && now <= endTime;
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
            var now = timeProvider.UtcNow;
            isActive = now >= startTime && now <= endTime;
            return true;
        }

        public bool TryGetEventWindow(string candidateEventId, out DateTime parsedStartTime, out DateTime parsedEndTime)
        {
            parsedStartTime = DateTime.MinValue;
            parsedEndTime = DateTime.MinValue;

            if (string.IsNullOrEmpty(candidateEventId)) return false;

            var idToken = candidateEventId;
            var prefix = $"{_eventIdPrefix}_";
            if (!string.IsNullOrEmpty(_eventIdPrefix))
            {
                if (!candidateEventId.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return false;
                }

                idToken = candidateEventId.Substring(prefix.Length);
            }

            if (!DateTime.TryParseExact(
                    idToken,
                    "yyyyMM",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var eventMonth))
            {
                return false;
            }

            parsedStartTime = new DateTime(eventMonth.Year, eventMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            parsedEndTime = parsedStartTime.AddMonths(1).AddSeconds(-1);
            return true;
        }

        public void Clear()
        {
            eventId = string.Empty;
            isActive = false;
            startTime = DateTime.MinValue;
            endTime = DateTime.MinValue;
        }

        public TimeSpan GetRemainingTime(ITimeProvider timeProvider)
        {
            var now = timeProvider.UtcNow;
            var remaining = endTime - now;
            return remaining.TotalSeconds < 1.0 ? TimeSpan.Zero : remaining;
        }

        public bool IsExpired(ITimeProvider timeProvider)
        {
            var now = timeProvider.UtcNow;
            return now >= endTime;
        }
    }
}
