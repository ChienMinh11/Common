using System;
using System.Globalization;
using ChieChie.Constracts;

namespace ChieChie.Shop
{
    public class ShopOfferAvailabilityService
    {
        private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly IShopTimeProvider _timeProvider;

        public ShopOfferAvailabilityService(IShopTimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? new SystemUtcShopTimeProvider();
        }

        public ShopOfferAvailabilityStatus GetStatus(IShopItemData itemData)
        {
            var availability = GetAvailability(itemData);
            if (availability == null || !availability.UseSchedule)
            {
                return CreateAlwaysAvailableStatus();
            }

            var status = new ShopOfferAvailabilityStatus
            {
                HasSchedule = true,
                ShowCountdown = availability.ShowCountdown
            };

            DateTime startTimeUtc;
            DateTime endTimeUtc;
            bool hasStartTime = TryReadOptionalUtc(availability.StartTimeUtc, out startTimeUtc);
            bool hasEndTime = TryReadOptionalUtc(availability.EndTimeUtc, out endTimeUtc);

            if (HasValue(availability.StartTimeUtc) && !hasStartTime)
            {
                return CreateInvalidStatus(availability, "Cau hinh start time cua goi khong hop le.");
            }

            if (HasValue(availability.EndTimeUtc) && !hasEndTime)
            {
                return CreateInvalidStatus(availability, "Cau hinh end time cua goi khong hop le.");
            }

            if (hasStartTime && hasEndTime && startTimeUtc >= endTimeUtc)
            {
                return CreateInvalidStatus(availability, "Start time phai nho hon end time.");
            }

            var nowUtc = _timeProvider.UtcNow;
            if (nowUtc.Kind != DateTimeKind.Utc)
            {
                nowUtc = nowUtc.ToUniversalTime();
            }

            bool notStarted = hasStartTime && nowUtc < startTimeUtc;
            bool expired = hasEndTime && nowUtc >= endTimeUtc;
            bool isAvailable = !notStarted && !expired;

            status.HasStartTime = hasStartTime;
            status.HasEndTime = hasEndTime;
            status.StartTimeUtc = startTimeUtc;
            status.EndTimeUtc = endTimeUtc;
            status.IsAvailable = isAvailable;
            status.ShouldShow = isAvailable || !availability.HideWhenUnavailable;
            status.CanPurchase = isAvailable || !availability.BlockPurchaseWhenUnavailable;

            if (notStarted)
            {
                status.TimeUntilStart = startTimeUtc - nowUtc;
                status.Reason = "Goi nay chua mo ban.";
            }
            else if (expired)
            {
                status.TimeRemaining = TimeSpan.Zero;
                status.Reason = "Goi nay da het han.";
            }
            else
            {
                status.TimeRemaining = hasEndTime ? endTimeUtc - nowUtc : TimeSpan.Zero;
                status.Reason = string.Empty;
            }

            return status;
        }

        public bool ShouldShow(IShopItemData itemData)
        {
            return GetStatus(itemData).ShouldShow;
        }

        public bool CanPurchase(IShopItemData itemData)
        {
            return GetStatus(itemData).CanPurchase;
        }

        public int GetPriority(IShopItemData itemData)
        {
            var availability = GetAvailability(itemData);
            return availability == null ? 0 : availability.Priority;
        }

        private static IShopOfferAvailability GetAvailability(IShopItemData itemData)
        {
            var availabilityData = itemData as IShopOfferAvailabilityData;
            return availabilityData?.OfferAvailability;
        }

        private static ShopOfferAvailabilityStatus CreateAlwaysAvailableStatus()
        {
            return new ShopOfferAvailabilityStatus
            {
                HasSchedule = false,
                IsAvailable = true,
                ShouldShow = true,
                CanPurchase = true,
                ShowCountdown = false,
                Reason = string.Empty
            };
        }

        private static ShopOfferAvailabilityStatus CreateInvalidStatus(IShopOfferAvailability availability, string reason)
        {
            return new ShopOfferAvailabilityStatus
            {
                HasSchedule = true,
                IsAvailable = false,
                ShouldShow = !availability.HideWhenUnavailable,
                CanPurchase = false,
                ShowCountdown = false,
                Reason = reason
            };
        }

        private static bool TryReadOptionalUtc(string rawTime, out DateTime utcTime)
        {
            utcTime = default(DateTime);
            if (!HasValue(rawTime)) return false;

            string trimmed = rawTime.Trim();
            long unixSeconds;
            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out unixSeconds))
            {
                try
                {
                    utcTime = UnixEpochUtc.AddSeconds(unixSeconds);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }
            }

            DateTimeOffset dateTimeOffset;
            if (DateTimeOffset.TryParse(
                    trimmed,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out dateTimeOffset))
            {
                utcTime = dateTimeOffset.UtcDateTime;
                return true;
            }

            return false;
        }

        private static bool HasValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
