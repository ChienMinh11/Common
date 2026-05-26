using GameCore.Runtime._Core.GameCore.Runtime.Scripts.ResourceSystem;
using UnityEngine;

namespace MyFramework
{
    public interface IResourceView
    {
        void SetResourceAmount<T>(T amount);
        void SetResourceAmountWithoutAnimation<T>(T amount);
        void SetResourceIcon(Sprite icon);
        void SetResourceName(string name);
        void ShowInsufficientMessage();
        void SetInfiniteState(bool isInfinite);
        void SetInfiniteIcon(Sprite icon); 
        void UpdateInfiniteTimeRemaining(float remainingTime);

        void OnMaxStackReached(ResourceType type);
    }
}
