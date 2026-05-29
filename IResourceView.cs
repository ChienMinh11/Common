using UnityEngine;

namespace ChieChie.Core
{
    public interface IResourceView
    {
        void SetResourceAmount(long amount);
        void SetResourceAmountWithoutAnimation(long amount);
        void SetResourceIcon(Sprite icon);
        void SetResourceName(string name);
        void ShowInsufficientMessage();
        void OnMaxStackReached(ResourceType type);
    }
}
