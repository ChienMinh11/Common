using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IAvatarPresenter
    {
        bool Initialize();
        List<AvatarModel> GetAllAvatars();
        List<AvatarModel> GetUnlockedAvatars();
        AvatarModel GetAvatar(int avatarId);
        Sprite GetAvatarSprite(int avatarId);
        bool UnlockAvatar(int avatarId);
        void AddAvatar(AvatarModel newAvatar);
        void UnlockAllAvatars();
        bool IsAvatarUnlocked(int avatarId);
    }
}
