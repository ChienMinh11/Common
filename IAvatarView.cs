using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IAvatarView
    {
        void DisplayAvatar(AvatarModel avatarInfo, Sprite avatarSprite, bool isSelected);
        void UpdateAvatarList(List<AvatarModel> avatars);
        void SetAvatarLockState(int avatarId, bool isUnlocked);
    }
}
