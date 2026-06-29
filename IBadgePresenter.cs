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
        bool UnlockBadge(int badgeId);
        void UnlockAllBadges();
        bool IsBadgeUnlocked(int badgeId);
    }
}