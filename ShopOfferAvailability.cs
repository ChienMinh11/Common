using System;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    [Serializable]
    public class ShopOfferAvailability : IShopOfferAvailability
    {
        [SerializeField] private bool useSchedule;
        [SerializeField] private ShopScheduleType scheduleType;
        [SerializeField] private string startTimeUtc;
        [SerializeField] private string endTimeUtc;
        [SerializeField] private long relativeDurationSeconds;
        [SerializeField] private string periodicDaysOfWeek;
        [SerializeField] private bool hideWhenUnavailable = true;
        [SerializeField] private bool blockPurchaseWhenUnavailable = true;
        [SerializeField] private bool showCountdown = true;
        [SerializeField] private int priority;

        public bool UseSchedule => useSchedule;
        public ShopScheduleType ScheduleType => scheduleType;
        public string StartTimeUtc => startTimeUtc;
        public string EndTimeUtc => endTimeUtc;
        public long RelativeDurationSeconds => relativeDurationSeconds;
        public string PeriodicDaysOfWeek => periodicDaysOfWeek;
        public bool HideWhenUnavailable => hideWhenUnavailable;
        public bool BlockPurchaseWhenUnavailable => blockPurchaseWhenUnavailable;
        public bool ShowCountdown => showCountdown;
        public int Priority => priority;
    }
}