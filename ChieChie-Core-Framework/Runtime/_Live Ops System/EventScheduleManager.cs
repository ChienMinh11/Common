using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class EventScheduleConfig
    {
        public ScheduleType scheduleType;
        public int durationInDays = 3; 
        public DayOfWeek weekStartDay = DayOfWeek.Monday; 
    }

    public enum ScheduleType
    {
        Manual, 
        Weekly, 
        Monthly, 
        Daily, 
        Quarterly, 
        SemiAnnual, 
    }

    [Serializable]
    public class ScheduledEvent
    {
        public string eventId;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;
        public ScheduleType scheduleType;

        public TimeSpan GetRemainingTime(DateTime currentTime)
        {
            var remaining = endTime - currentTime;
            return remaining.TotalSeconds < 1.0 ? TimeSpan.Zero : remaining;
        }

        public bool IsExpired(DateTime currentTime)
        {
            return currentTime >= endTime;
        }
    }

    public class EventScheduleManager
    {
        private EventScheduleConfig config;
        private InternetTimeService internetTimeService;
        private readonly List<ScheduledEvent> scheduledEvents = new();

        public IReadOnlyList<ScheduledEvent> AllScheduledEvents => scheduledEvents;

        public void Initialize(EventScheduleConfig scheduleConfig, InternetTimeService internetTimeService)
        {
            config = scheduleConfig;
            this.internetTimeService = internetTimeService;
            GenerateScheduleForCurrentPeriod();
        }

        private DateTime GetCurrentTime()
        {
            if (internetTimeService?.IsTimeValid == true)
            {
                return internetTimeService.GetCurrentTime();
            }
    
            Debug.LogWarning("[EventScheduleManager] Internet time not available, using UTC time");
            return DateTime.UtcNow; // Đồng nhất sử dụng UTC thay vì local thời gian máy
        }

        public bool IsWeekendActive()
        {
            var now = GetCurrentTime();
            return now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
        }

        private void GenerateScheduleForCurrentPeriod()
        {
            scheduledEvents.Clear();
            var now = GetCurrentTime(); 
            
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1).AddSeconds(-1); // 23:59:59 ngày cuối tháng

            switch (config.scheduleType)
            {
                case ScheduleType.Monthly:
                    GenerateMonthlySchedule(now, monthStart, monthEnd);
                    break;
                case ScheduleType.Weekly:
                    GenerateWeeklySchedule(now, monthStart, monthEnd);
                    break;
                case ScheduleType.Daily:
                    GenerateDailySchedule(now, monthStart, monthEnd);
                    break;
                case ScheduleType.Quarterly:
                    GenerateQuarterlySchedule(now);
                    break;
                case ScheduleType.SemiAnnual:
                    GenerateSemiAnnualSchedule(now);
                    break;
            }
        }

        private void GenerateMonthlySchedule(DateTime now, DateTime monthStart, DateTime monthEnd)
        {
            scheduledEvents.Add(new ScheduledEvent
            {
                eventId = $"Monthly_{monthStart:yyyyMM}",
                startTime = monthStart,
                endTime = monthEnd,
                scheduleType = ScheduleType.Monthly,
                isActive = now >= monthStart && now <= monthEnd
            });
        }

        private void GenerateWeeklySchedule(DateTime now, DateTime monthStart, DateTime monthEnd)
        {
            var current = GetWeekStart(monthStart, config.weekStartDay);
            var weekIndex = 1;

            while (current <= monthEnd)
            {
                var weekEnd = current.AddDays(7).AddSeconds(-1);

                // Giới hạn trong tháng nếu cần cấu hình chặt chẽ theo tháng
                if (weekEnd > monthEnd) weekEnd = monthEnd;

                scheduledEvents.Add(new ScheduledEvent
                {
                    eventId = $"Weekly_{monthStart:yyyyMM}_W{weekIndex}",
                    startTime = current,
                    endTime = weekEnd,
                    scheduleType = ScheduleType.Weekly,
                    isActive = now >= current && now <= weekEnd
                });
                
                current = current.AddDays(7);
                weekIndex++;
            }
        }

        private void GenerateDailySchedule(DateTime now, DateTime monthStart, DateTime monthEnd)
        {
            var current = monthStart;
            var index = 1;

            // Tối ưu hóa: Dùng vòng lặp tịnh tiến thay vì tính toán toán học phức tạp
            while (current <= monthEnd)
            {
                var eventEnd = current.AddDays(config.durationInDays).AddSeconds(-1);
                
                if (eventEnd > monthEnd) eventEnd = monthEnd;

                scheduledEvents.Add(new ScheduledEvent
                {
                    eventId = $"Daily_{monthStart:yyyyMM}_D{index}",
                    startTime = current,
                    endTime = eventEnd,
                    scheduleType = ScheduleType.Daily,
                    isActive = now >= current && now <= eventEnd
                });

                current = current.AddDays(config.durationInDays);
                index++;
            }
        }

        private void GenerateQuarterlySchedule(DateTime now)
        {
            var quarter = (now.Month - 1) / 3 + 1;
            var quarterStartMonth = (quarter - 1) * 3 + 1;
    
            var quarterStart = new DateTime(now.Year, quarterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var quarterEnd = quarterStart.AddMonths(3).AddSeconds(-1);
    
            scheduledEvents.Add(new ScheduledEvent
            {
                eventId = $"Quarterly_{now.Year}_Q{quarter}",
                startTime = quarterStart,
                endTime = quarterEnd,
                scheduleType = ScheduleType.Quarterly,
                isActive = now >= quarterStart && now <= quarterEnd
            });
        }

        private void GenerateSemiAnnualSchedule(DateTime now)
        {
            var half = now.Month <= 6 ? 1 : 2;
            var halfStartMonth = half == 1 ? 1 : 7;
    
            var halfStart = new DateTime(now.Year, halfStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var halfEnd = halfStart.AddMonths(6).AddSeconds(-1);
    
            scheduledEvents.Add(new ScheduledEvent
            {
                eventId = $"SemiAnnual_{now.Year}_H{half}",
                startTime = halfStart,
                endTime = halfEnd,
                scheduleType = ScheduleType.SemiAnnual,
                isActive = now >= halfStart && now <= halfEnd
            });
        }

        private DateTime GetWeekStart(DateTime date, DayOfWeek startDay)
        {
            var daysFromStartOfWeek = (int)date.DayOfWeek - (int)startDay;
            if (daysFromStartOfWeek < 0)
                daysFromStartOfWeek += 7;

            return date.AddDays(-daysFromStartOfWeek);
        }

        public ScheduledEvent GetCurrentActiveEvent()
        {
            var now = GetCurrentTime();
            // Tối ưu: Dùng vòng lặp for thay vì LINQ (FirstOrDefault) để tránh sinh rác (garbage alloc)
            for (int i = 0; i < scheduledEvents.Count; i++)
            {
                if (scheduledEvents[i].isActive && !scheduledEvents[i].IsExpired(now))
                {
                    return scheduledEvents[i];
                }
            }
            return null;
        }

        public void RefreshActiveStatus()
        {
            var currentTime = GetCurrentTime();
            for (int i = 0; i < scheduledEvents.Count; i++)
            {
                var evt = scheduledEvents[i];
                evt.isActive = currentTime >= evt.startTime && currentTime <= evt.endTime;
            }
        }
    }
}