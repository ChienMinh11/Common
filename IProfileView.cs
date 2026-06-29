using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IProfileView
    {
        event Action OnEditNameRequested;
        event Action<string> OnTemporaryNameChanged;
        event Action<int> OnAvatarSelected;
        event Action OnSaveRequested;
        event Action OnCloseRequested;
   
        void ShowProfileData(string playerName, Sprite avatarSprite);
        void UpdatePlayerNameDisplay(string name);
        void UpdateAvatarDisplay(int avatarId, Sprite avatarSprite);
        void PopulateAvatarGrid(List<AvatarModel> avatars, Dictionary<int, Sprite> avatarSprites);
        void SetSaveButtonInteractable(bool interactable);
    }
}
