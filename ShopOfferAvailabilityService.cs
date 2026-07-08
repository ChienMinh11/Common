using System;
using System.Globalization;
using System.Collections.Generic;
using ChieChie.Constracts;

namespace ChieChie.Shop
{
    public class ShopOfferAvailabilityService
    {
        private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly ITimeProvider _timeProvider;

        public ShopOfferAvailabilityService(ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider;
        }

        public ShopOfferAvailabilityStatus GetStatus(IShopItemData itemData, DateTime? dynamicTriggerTime = null)
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

            var nowUtc = _timeProvider != null ? _timeProvider.UtcNow : DateTime.UtcNow;
            if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = nowUtc.ToUniversalTime();

            switch (availability.ScheduleType)
            {
                case ShopScheduleType.RelativeDuration:
                    ProcessRelativeSchedule(availability, status, nowUtc, dynamicTriggerTime);
                    break;

                case ShopScheduleType.PeriodicWeekly:
                    ProcessPeriodicSchedule(availability, status, nowUtc);
                    break;

                case ShopScheduleType.FixedSchedule:
                default:
                    ProcessFixedSchedule(availability, status, nowUtc);
                    break;
            }

            return status;
        }

        private void ProcessFixedSchedule(IShopOfferAvailability availability, ShopOfferAvailabilityStatus status,
            DateTime nowUtc)
        {
            DateTime startTimeUtc;
            DateTime endTimeUtc;
            bool hasStartTime = TryReadOptionalUtc(availability.StartTimeUtc, out startTimeUtc);
            bool hasEndTime = TryReadOptionalUtc(availability.EndTimeUtc, out endTimeUtc);

            if (HasValue(availability.StartTimeUtc) && !hasStartTime)
            {
                SetInvalid(status, availability, "Cau hinh start time khong hop le.");
                return;
            }

            if (HasValue(availability.EndTimeUtc) && !hasEndTime)
            {
                SetInvalid(status, availability, "Cau hinh end time khong hop le.");
                return;
            }

            if (hasStartTime && hasEndTime && startTimeUtc >= endTimeUtc)
            {
                SetInvalid(status, availability, "Start time phai nho hon end time.");
                return;
            }

            status.HasStartTime = hasStartTime;
            status.HasEndTime = hasEndTime;
            status.StartTimeUtc = startTimeUtc;
            status.EndTimeUtc = endTimeUtc;

            EvaluateAvailabilityTimings(status, nowUtc, startTimeUtc, endTimeUtc, hasStartTime, hasEndTime,
                availability);
        }

        private void ProcessRelativeSchedule(IShopOfferAvailability availability, ShopOfferAvailabilityStatus status,
            DateTime nowUtc, DateTime? dynamicTriggerTime)
        {
            DateTime startPoint = dynamicTriggerTime ?? nowUtc;
            DateTime endPoint = startPoint.AddSeconds(availability.RelativeDurationSeconds);

            status.HasStartTime = true;
            status.HasEndTime = true;
            status.StartTimeUtc = startPoint;
            status.EndTimeUtc = endPoint;

            EvaluateAvailabilityTimings(status, nowUtc, startPoint, endPoint, true, true, availability);
        }

        private void ProcessPeriodicSchedule(IShopOfferAvailability availability, ShopOfferAvailabilityStatus status,
            DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(availability.PeriodicDaysOfWeek))
            {
                SetInvalid(status, availability, "Chua cau hinh cac ngay lap lai trong tuan.");
                return;
            }

            var activeDays = new HashSet<DayOfWeek>();
            var tokens = availability.PeriodicDaysOfWeek.Split(',');
            foreach (var token in tokens)
            {
                if (Enum.TryParse<DayOfWeek>(token.Trim(), true, out var day))
                {
                    activeDays.Add(day);
                }
            }

            bool isTodayAvailable = activeDays.Contains(nowUtc.DayOfWeek);
            status.IsAvailable = isTodayAvailable;
            status.ShouldShow = isTodayAvailable || !availability.HideWhenUnavailable;
            status.CanPurchase = isTodayAvailable || !availability.BlockPurchaseWhenUnavailable;

            if (isTodayAvailable)
            {
             
                int continuousDaysCount = 1;

                for (int i = 1; i <= 7; i++)
                {
                   
                    DayOfWeek nextDay = nowUtc.AddDays(i).DayOfWeek;
                    if (activeDays.Contains(nextDay))
                    {
                        continuousDaysCount++;
                    }
                    else
                    {
                        
                        break;
                    }
                }

                DateTime endOfContinuousPeriod = nowUtc.Date.AddDays(continuousDaysCount);

                status.TimeRemaining = endOfContinuousPeriod - nowUtc;
                status.HasEndTime = true;
                status.EndTimeUtc = endOfContinuousPeriod;
                status.Reason = string.Empty;
            }
            else
            {
                status.TimeRemaining = TimeSpan.Zero;
                status.Reason = "Goi nay khong mo ban trong ngay hom nay.";
            }
        }

        private void EvaluateAvailabilityTimings(ShopOfferAvailabilityStatus status, DateTime nowUtc, DateTime start,
            DateTime end, bool hasStart, bool hasEnd, IShopOfferAvailability availability)
        {
            bool notStarted = hasStart && nowUtc < start;
            bool expired = hasEnd && nowUtc >= end;
            bool isAvailable = !notStarted && !expired;

            status.IsAvailable = isAvailable;
            status.ShouldShow = isAvailable || !availability.HideWhenUnavailable;
            status.CanPurchase = isAvailable || !availability.BlockPurchaseWhenUnavailable;

            if (notStarted)
            {
                status.TimeUntilStart = start - nowUtc;
                status.Reason = "Goi nay chua mo ban.";
            }
            else if (expired)
            {
                status.TimeRemaining = TimeSpan.Zero;
                status.Reason = "Goi nay da het han.";
            }
            else
            {
                status.TimeRemaining = hasEnd ? end - nowUtc : TimeSpan.Zero;
                status.Reason = string.Empty;
            }
        }

        private void SetInvalid(ShopOfferAvailabilityStatus status, IShopOfferAvailability availability, string reason)
        {
            status.IsAvailable = false;
            status.ShouldShow = !availability.HideWhenUnavailable;
            status.CanPurchase = false;
            status.ShowCountdown = false;
            status.Reason = reason;
        }

        public bool ShouldShow(IShopItemData itemData) => GetStatus(itemData).ShouldShow;
        public bool CanPurchase(IShopItemData itemData) => GetStatus(itemData).CanPurchase;

        public int GetPriority(IShopItemData itemData)
        {
            var av = GetAvailability(itemData);
            return av == null ? 0 : av.Priority;
        }

        private static IShopOfferAvailability GetAvailability(IShopItemData itemData) =>
            (itemData as IShopOfferAvailabilityData)?.OfferAvailability;

        private static ShopOfferAvailabilityStatus CreateAlwaysAvailableStatus() => new ShopOfferAvailabilityStatus
        {
            HasSchedule = false, IsAvailable = true, ShouldShow = true, CanPurchase = true, ShowCountdown = false,
            Reason = string.Empty
        };

        private static bool HasValue(string value) => !string.IsNullOrWhiteSpace(value);

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
            if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTimeOffset))
            {
                utcTime = dateTimeOffset.UtcDateTime;
                return true;
            }

            return false;
        }
    }
}
