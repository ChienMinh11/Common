using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Constracts
{
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

    // Thêm mới Struct hiển thị cho Frame
    public struct FrameDisplayData
    {
        public int Id;
        public string Name;
        public Sprite Sprite;
        public bool IsUnlocked;

        public FrameDisplayData(int id, string name, Sprite sprite, bool isUnlocked)
        {
            Id = id;
            Name = name;
            Sprite = sprite;
            IsUnlocked = isUnlocked;
        }
    }

    // Thêm mới Struct hiển thị cho Badge
    public struct BadgeDisplayData
    {
        public int Id;
        public string Name;
        public Sprite Sprite;
        public bool IsUnlocked;

        public BadgeDisplayData(int id, string name, Sprite sprite, bool isUnlocked)
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
        event Action<int> OnFrameSelected; // Thêm mới
        event Action<int> OnBadgeSelected; // Thêm mới
        event Action OnSaveRequested;
        event Action OnCloseRequested;
   
        void ShowProfileData(string playerName, Sprite avatarSprite, Sprite frameSprite, Sprite badgeSprite); // Cập nhật
        void UpdatePlayerNameDisplay(string name);
        void UpdateAvatarDisplay(int avatarId, Sprite avatarSprite);
        void UpdateFrameDisplay(int frameId, Sprite frameSprite); // Thêm mới
        void UpdateBadgeDisplay(int badgeId, Sprite badgeSprite); // Thêm mới
        
        void PopulateAvatarGrid(List<AvatarDisplayData> avatars); 
        void PopulateFrameGrid(List<FrameDisplayData> frames);   // Thêm mới
        void PopulateBadgeGrid(List<BadgeDisplayData> badges);   // Thêm mới
        void SetSaveButtonInteractable(bool interactable);
    }
}