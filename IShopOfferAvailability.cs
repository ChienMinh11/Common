namespace ChieChie.Constracts
{
    public interface IShopOfferAvailability
    {
        bool UseSchedule { get; }
        string StartTimeUtc { get; }
        string EndTimeUtc { get; }
        bool HideWhenUnavailable { get; }
        bool BlockPurchaseWhenUnavailable { get; }
        bool ShowCountdown { get; }
        int Priority { get; }
    }
}
