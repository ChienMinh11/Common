using UnityEngine;

namespace ChieChie.Core
{
    public enum SharedEventType
    {
        None = 0,
        RequestAddResource = 1,
        RequestBuySuccess = 2,
        OnInfiniteDurationAdded = 3,  
        OnInfiniteDurationExpired = 4,
        OnShopRewardsNotificationRequested = 5
    }
}