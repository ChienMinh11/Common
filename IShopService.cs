using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Shop
{
    public interface IShopService
    {
        void ResetPackTimeLimited(ProductID id);
        
        event Action<ProductID> OnBuySuccess;
        event Action<List<ResourceRewardCommand>> OnRequestAddResource;
        event Action<ShopNotificationEventData> OnShopRewardsNotificationRequested;
    }
}
