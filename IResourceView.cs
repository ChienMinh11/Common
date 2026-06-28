using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IResourceView
    {
        void SetResourceAmount(long amount);
        void SetResourceAmountWithoutAnimation(long amount);
        void SetResourceIcon(Sprite icon);
        void SetResourceName(string name);
        void ShowInsufficientMessage();
        void OnMaxStackReached(string resourceKey);
        void SetInfiniteStatus(bool isInfinite);
        void UpdateInfinityRemainingTime(string formattedTime);
        void SetRegenStatusActive(bool isActive);
        void SetRegenStatusText(string text);
    }
}
