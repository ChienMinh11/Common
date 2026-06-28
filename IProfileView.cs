using UnityEngine;

namespace ChieChie.Profile
{
    public interface IProfileView
    {
        void UpdatePlayerName(string name);
        void UpdateAvatarDisplay(int avatarId, Sprite avatarSprite);
        void ShowProfileData(ProfileModel profile, Sprite avatarSprite);
        void CloseView();
    }
}
