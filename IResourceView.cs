using System;
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
        void UpdateInfinityStatus(bool isInfinite, DateTime expirationTime);
        void UpdateRegenStatus(bool isRegenEnabled, bool isMaxStack, DateTime nextRegenTime);
    }
}
