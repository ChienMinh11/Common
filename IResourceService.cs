using System;

namespace ChieChie.Constracts
{
    public interface IResourceService
    {
        void RegisterView(string resourceKey, IResourceView view);
        void UnregisterView(IResourceView view);

        void AddResource(string resourceKey, long amount, bool delayUpdate = false);
        bool SpendResource(string resourceKey, long amount);
        long GetCurrentAmount(string resourceKey);
        bool IsAtMaxStack(string resourceKey);
        long GetMaxStack(string resourceKey);
 
        void SetMaxStackAndFill(string resourceKey, long newMaxStack, bool fillFull = false);
        void ProcessPendingUpdate(string resourceKey, long amountIncrement = 0);
        void ForceUpdateAllView();

        void AddInfiniteDuration(string resourceKey, TimeSpan duration, bool delayUpdate = false);
        bool IsCurrentlyInfinite(string resourceKey);
        TimeSpan GetRemainingInfiniteTime(string resourceKey);

        bool IsRegenEnabled(string resourceKey);
        DateTime GetNextRegenTime(string resourceKey);
        void SetRegenStatus(string resourceKey, bool isEnabled);
        
        event Action<string> OnInfiniteExpired;
        event Action<string,bool> OnInfiniteAdded;
        
       void OnAppQuit();

       void OnAppPause(bool pauseStatus);

    }
}
