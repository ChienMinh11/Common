using System;
using ChieChie.Core;

namespace ChieChie.Resource
{
    public interface IResourceService
    {
     
        IResourcePresenter RegisterView(string resourceKey, IResourceView view);
        void UnregisterPresenter(IResourcePresenter presenter);
     
        void AddResource(string resourceKey, long amount, bool delayUpdate = false);
        bool SpendResource(string resourceKey, long amount);
        long GetCurrentAmount(string resourceKey);
        bool IsAtMaxStack(string resourceKey);
        long GetMaxStack(string resourceKey);
 
        void SetMaxStackAndFill(string resourceKey, long newMaxStack, bool fillFull = false);
        void ProcessPendingUpdate(string resourceKey);
        void ForceUpdateAllView();

        void AddInfiniteDuration(string resourceKey, TimeSpan duration);
        bool IsCurrentlyInfinite(string resourceKey);
        TimeSpan GetRemainingInfiniteTime(string resourceKey);
        
        ResourceConfig GetConfig();

        bool IsRegenEnabled(string resourceKey);
        DateTime GetNextRegenTime(string resourceKey);
        void SetRegenStatus(string resourceKey, bool isEnabled);
    }
}
