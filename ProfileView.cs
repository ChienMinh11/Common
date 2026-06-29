using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class ProfileView : MonoBehaviour, IProfileView
    {
        [Header("Profile Display")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image frameImage;  
        [SerializeField] private Image badgeImage;  
        [SerializeField] private Button editNameButton;

        [Header("Navigation Controller")]
        [SerializeField] private ProfileTabNavigation profileTabNavigation; 

        [Header("Grid Prefabs & Containers")]
        [SerializeField] private Transform avatarGridContainer;
        [SerializeField] private AvatarView avatarItemPrefab;
        [SerializeField] private Transform frameGridContainer;
        [SerializeField] private FrameItemView frameItemPrefab;
        [SerializeField] private Transform badgeGridContainer;
        [SerializeField] private BadgeItemView badgeItemPrefab;

        public event Action OnEditNameRequested;
        public event Action<string> OnTemporaryNameChanged;
        public event Action<int> OnAvatarSelected;
        public event Action<int> OnFrameSelected; 
        public event Action<int> OnBadgeSelected; 
        public event Action OnSaveRequested;
        public event Action OnCloseRequested;

        private List<AvatarView> _avatarItems = new List<AvatarView>();
        private List<FrameItemView> _frameItems = new List<FrameItemView>(); 
        private List<BadgeItemView> _badgeItems = new List<BadgeItemView>(); 
        
        private int _currentAvatarId;
        private int _currentFrameId; 
        private int _currentBadgeId; 
        private IPopupService _popupController;
        private string _cachedPlayerName;

        [Inject]
        public void Construct(IPopupService popupService) => _popupController = popupService; //

        private void Start()
        {
            if (editNameButton != null)
            {
                editNameButton.onClick.RemoveAllListeners(); //
                editNameButton.onClick.AddListener(() => OnEditNameRequested?.Invoke());
            }

            if (profileTabNavigation != null)
            {
                // Nếu sau này bạn cần làm gì đó khi chuyển tab (ví dụ: phát âm thanh), có thể đăng ký ở đây
                profileTabNavigation.OnTabChanged += HandleProfileTabChanged;
            }
        }

        private void OnDestroy()
        {
            if (profileTabNavigation != null)
            {
                profileTabNavigation.OnTabChanged -= HandleProfileTabChanged;
            }
        }

        private void HandleProfileTabChanged(int tabIndex)
        {
            // Xử lý bổ sung khi tab thay đổi (nếu cần)
        }

        public void ShowProfileData(string playerName, Sprite avatarSprite, Sprite frameSprite, Sprite badgeSprite)
        {
            _cachedPlayerName = playerName; 
            UpdatePlayerNameDisplay(playerName);
            if (avatarImage != null && avatarSprite != null) avatarImage.sprite = avatarSprite; 
            if (frameImage != null && frameSprite != null) frameImage.sprite = frameSprite; 
    
            if (badgeImage != null)
            {
                badgeImage.sprite = badgeSprite;
                badgeImage.gameObject.SetActive(badgeSprite != null);
            }
        }

        public void UpdatePlayerNameDisplay(string name)
        {
            if (playerNameText != null) playerNameText.text = name; //
            _cachedPlayerName = name; //
        }

        public void UpdateAvatarDisplay(int avatarId, Sprite avatarSprite)
        {
            _currentAvatarId = avatarId; //
            if (avatarImage != null && avatarSprite != null) avatarImage.sprite = avatarSprite; //
            foreach (var item in _avatarItems)
            {
                if (item != null) item.SetSelected(item.GetAvatarId() == _currentAvatarId); //
            }
        }

        public void UpdateFrameDisplay(int frameId, Sprite frameSprite) 
        {
            _currentFrameId = frameId;
            if (frameImage != null && frameSprite != null) frameImage.sprite = frameSprite;
            foreach (var item in _frameItems)
            {
                if (item != null) item.SetSelected(item.GetFrameId() == _currentFrameId);
            }
        }

        public void UpdateBadgeDisplay(int badgeId, Sprite badgeSprite) 
        {
            _currentBadgeId = badgeId;
            if (badgeImage != null)
            {
                badgeImage.sprite = badgeSprite;
                badgeImage.gameObject.SetActive(badgeSprite != null);
            }
    
            foreach (var item in _badgeItems)
            {
                if (item != null) item.SetSelected(item.GetBadgeId() == _currentBadgeId);
            }
        }

        public void PopulateAvatarGrid(List<AvatarDisplayData> avatars)
        {
            ClearGrid(_avatarItems); //
            foreach (var avatar in avatars) //
            {
                if (avatarItemPrefab != null && avatarGridContainer != null) //
                {
                    AvatarView newItem = Instantiate(avatarItemPrefab, avatarGridContainer); //
                    newItem.Initialize(avatar.Id, avatar.Name, avatar.Sprite, avatar.IsUnlocked, avatar.Id == _currentAvatarId, OnAvatarGridItemSelected); //
                    _avatarItems.Add(newItem); //
                }
            }
        }

        public void PopulateFrameGrid(List<FrameDisplayData> frames) 
        {
            ClearGrid(_frameItems);
            foreach (var frame in frames)
            {
                if (frameItemPrefab != null && frameGridContainer != null)
                {
                    FrameItemView newItem = Instantiate(frameItemPrefab, frameGridContainer);
                    newItem.Initialize(frame.Id, frame.Name, frame.Sprite, frame.IsUnlocked, frame.Id == _currentFrameId, OnFrameGridItemSelected);
                    _frameItems.Add(newItem);
                }
            }
        }

        public void PopulateBadgeGrid(List<BadgeDisplayData> badges) 
        {
            ClearGrid(_badgeItems);
            foreach (var badge in badges)
            {
                if (badgeItemPrefab != null && badgeGridContainer != null)
                {
                    BadgeItemView newItem = Instantiate(badgeItemPrefab, badgeGridContainer);
                    newItem.Initialize(badge.Id, badge.Name, badge.Sprite, badge.IsUnlocked, badge.Id == _currentBadgeId, OnBadgeGridItemSelected);
                    _badgeItems.Add(newItem);
                }
            }
        }

        private void ClearGrid<T>(List<T> list) where T : MonoBehaviour
        {
            foreach (var item in list) if (item != null) Destroy(item.gameObject); //
            list.Clear(); //
        }

        public void SetSaveButtonInteractable(bool interactable) { } //

        private void OnAvatarGridItemSelected(int avatarId) => OnAvatarSelected?.Invoke(avatarId); //
        private void OnFrameGridItemSelected(int frameId) => OnFrameSelected?.Invoke(frameId); 
        private void OnBadgeGridItemSelected(int badgeId) => OnBadgeSelected?.Invoke(badgeId); 
    }
}