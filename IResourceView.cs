using System;
using ChieChie.MVP;
using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IResourceView : IView
    {
        string ResourceKey { get; }

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
