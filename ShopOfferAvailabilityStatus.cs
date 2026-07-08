using System;

namespace ChieChie.Shop
{
    public class ShopOfferAvailabilityStatus
    {
        public bool HasSchedule { get; set; }
        public bool IsAvailable { get; set; }
        public bool ShouldShow { get; set; }
        public bool CanPurchase { get; set; }
        public bool ShowCountdown { get; set; }
        public bool HasStartTime { get; set; }
        public bool HasEndTime { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public TimeSpan TimeUntilStart { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public string Reason { get; set; }
    }
}
