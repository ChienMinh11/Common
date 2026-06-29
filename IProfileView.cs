using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    // Struct trung gian chỉ chứa thông tin hiển thị thuần túy cho View
    public struct AvatarDisplayData
    {
        public int Id;
        public string Name;
        public Sprite Sprite;
        public bool IsUnlocked;

        public AvatarDisplayData(int id, string name, Sprite sprite, bool isUnlocked)
        {
            Id = id;
            Name = name;
            Sprite = sprite;
            IsUnlocked = isUnlocked;
        }
    }

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
        void PopulateAvatarGrid(List<AvatarDisplayData> avatars); // Sử dụng struct mới thay cho AvatarModel
        void SetSaveButtonInteractable(bool interactable);
    }
}