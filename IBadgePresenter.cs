using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IBadgePresenter
    {
        event Action OnBadgeListUpdated;
        event Action<BadgeModel> OnBadgeUnlocked;

        bool Initialize();
        List<BadgeModel> GetAllBadges();
        BadgeModel GetBadge(int badgeId);
        Sprite GetBadgeSprite(int badgeId);
        GameObject GetBadgePrefab(int frameId);
        bool UnlockBadge(int badgeId);
        void UnlockAllBadges();
        bool IsBadgeUnlocked(int badgeId);
    }
}