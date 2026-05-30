using System;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IResourceManager
    {
        IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view);
        void UnregisterPresenter(IResourcePresenter presenter);

        void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false);
        bool SpendResource(ResourceType resourceType, long amount);
        long GetCurrentAmount(ResourceType resourceType);
        bool IsAtMaxStack(ResourceType resourceType);
        long GetMaxStack(ResourceType resourceType);
        void SetMaxStackAndFill(ResourceType resourceType, long newMaxStack, bool fillFull = false);
        void ProcessPendingUpdate(ResourceType resourceType);
        void ForceUpdateAllView();

        void AddInfiniteDuration(ResourceType resourceType, TimeSpan duration);
        bool IsCurrentlyInfinite(ResourceType resourceType);
        TimeSpan GetRemainingInfiniteTime(ResourceType resourceType);
        ResourceConfig GetConfig();
    }
}
