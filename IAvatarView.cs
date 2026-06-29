using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IAvatarView
    {
        void DisplayAvatar(int id, string name, Sprite avatarSprite, bool isSelected, bool isUnlocked);
        void UpdateAvatarList(List<int> avatarIds);
        void SetAvatarLockState(int avatarId, bool isUnlocked);
    }
}