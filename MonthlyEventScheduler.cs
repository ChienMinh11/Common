using System;
using ChieChie.Constracts;

namespace ChieChie.GenericLiveOps
{
    [Serializable]
    public class MonthlyEventScheduler : EventSchedulerBase
    {
        private const string EventIdDateFormat = "yyyyMM";
        private readonly string _eventIdPrefix;

        public MonthlyEventScheduler(string eventIdPrefix)
        {
            _eventIdPrefix = eventIdPrefix;
            eventId = string.Empty;
        }

        public void UpdateMonthlySchedule(ITimeProvider timeProvider)
        {
            UpdateSchedule(timeProvider);
        }

        public override bool TryGetEventWindow(string candidateEventId, out DateTime parsedStartTime, out DateTime parsedEndTime)
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
                    EventIdDateFormat,
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

        protected override bool TryCreateCurrentEvent(
            DateTime now,
            out string currentEventId,
            out DateTime currentStartTime,
            out DateTime currentEndTime)
        {
            currentStartTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            currentEndTime = currentStartTime.AddMonths(1).AddSeconds(-1);
            currentEventId = BuildEventId(currentStartTime);
            return true;
        }

        private string BuildEventId(DateTime eventStartTime)
        {
            var idToken = eventStartTime.ToString(EventIdDateFormat, System.Globalization.CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(_eventIdPrefix) ? idToken : $"{_eventIdPrefix}_{idToken}";
        }
    }
}
