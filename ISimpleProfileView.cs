using UnityEngine;

namespace ChieChie.Constracts
{
    public interface ISimpleProfileView
    {
        void UpdateAvatar(Sprite avatarSprite);
        void UpdateFrame(int frameId, GameObject framePrefab);
        void UpdateBadge(int badgeId, GameObject badgePrefab);
    }
}
