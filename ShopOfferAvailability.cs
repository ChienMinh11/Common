using System;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Shop
{
    [Serializable]
    public class ShopOfferAvailability : IShopOfferAvailability
    {
        [SerializeField] private bool useSchedule;

        [Tooltip("UTC start time. Empty means available immediately. Supports ISO text like 2026-07-08T00:00:00Z or Unix seconds.")]
        [SerializeField] private string startTimeUtc;

        [Tooltip("UTC end time. Empty means no end time. Supports ISO text like 2026-07-15T00:00:00Z or Unix seconds.")]
        [SerializeField] private string endTimeUtc;

        [SerializeField] private bool hideWhenUnavailable = true;
        [SerializeField] private bool blockPurchaseWhenUnavailable = true;
        [SerializeField] private bool showCountdown = true;
        [SerializeField] private int priority;

        public bool UseSchedule => useSchedule;
        public string StartTimeUtc => startTimeUtc;
        public string EndTimeUtc => endTimeUtc;
        public bool HideWhenUnavailable => hideWhenUnavailable;
        public bool BlockPurchaseWhenUnavailable => blockPurchaseWhenUnavailable;
        public bool ShowCountdown => showCountdown;
        public int Priority => priority;
    }
}
