namespace ChieChie.Constracts
{
    public enum ShopScheduleType
    {
        FixedSchedule = 0,    
        RelativeDuration = 1, 
        PeriodicWeekly = 2    
    }

    public interface IShopOfferAvailability
    {
        bool UseSchedule { get; }
        ShopScheduleType ScheduleType { get; } 
        
        // Fixed Schedule
        string StartTimeUtc { get; }
        string EndTimeUtc { get; }

        long RelativeDurationSeconds { get; }

        string PeriodicDaysOfWeek { get; }
        
        bool HideWhenUnavailable { get; }
        bool BlockPurchaseWhenUnavailable { get; }
        bool ShowCountdown { get; }
        int Priority { get; }
    }
}