using System;

namespace ChieChie.GamePass
{
    [Serializable]
    public class PassEventScheduler
    {
        public string eventId;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;

        public void UpdateMonthlySchedule(DateTime currentTime)
        {
            var now = currentTime.Kind == DateTimeKind.Utc ? currentTime : currentTime.ToUniversalTime();
            startTime = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            endTime = startTime.AddMonths(1).AddSeconds(-1);
            eventId = $"GamePass_{startTime:yyyyMM}";
            isActive = now >= startTime && now <= endTime;
        }
        
        public TimeSpan GetRemainingTime(DateTime currentTime)
        {
            var now = currentTime.Kind == DateTimeKind.Utc ? currentTime : currentTime.ToUniversalTime();
            var remaining = endTime - now;
            return remaining.TotalSeconds < 1.0 ? TimeSpan.Zero : remaining;
        }
        public bool IsExpired(DateTime currentTime)
        {
            var now = currentTime.Kind == DateTimeKind.Utc ? currentTime : currentTime.ToUniversalTime();
            return now >= endTime;
        }
    }
}