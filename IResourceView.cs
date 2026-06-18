using UnityEngine;

namespace ChieChie.Resource
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
    }
}
