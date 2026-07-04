using System;
using ChieChie.Constracts;

namespace ChieChie.GamePass
{
    [Serializable]
    public class PassEventScheduler
    {
        public string eventId;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;

        public void UpdateMonthlySchedule(ITimeProvider timeProvider)
        {
            var now = timeProvider.UtcNow;
            startTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            endTime = startTime.AddMonths(1).AddSeconds(-1);
            eventId = $"GamePass_{startTime:yyyyMM}";
            isActive = now >= startTime && now <= endTime;
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