using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IShopService
    {
        void RegisterView(IShopView view);
        void UnregisterView(IShopView view);
        Sprite GetIconResourceReward(string resourceType, bool isInfinite);
        bool IsItemOwned(string productId);
        bool IsOfferAvailable(string productId);
        bool TryGetOfferTimeRemaining(string productId, out TimeSpan remaining);
        void RefreshShopItems();
        void ResetPackTimeLimited(string id);
        event Action<string> OnBuySuccess;
        event Action<List<ResourceRewardCommand>> OnRequestAddResource;
        event Action<IShopNotificationEventData> OnShopRewardsNotificationRequested;
        
        
    }
}
