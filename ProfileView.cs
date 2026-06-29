using System;
using System.Collections.Generic;
using ChieChie.Core;
using ChieChie.Profile;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class ProfileView : MonoBehaviour, IProfileView
    {
        [Header("Profile Display")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Button editNameButton;

        [Header("Avatar Grid")]
        [SerializeField] private Transform avatarGridContainer;
        [SerializeField] private AvatarView avatarItemPrefab;

        public event Action OnEditNameRequested;
        public event Action<string> OnTemporaryNameChanged;
        public event Action<int> OnAvatarSelected;
        public event Action OnSaveRequested;
        public event Action OnCloseRequested;

        private List<AvatarView> _avatarItems = new List<AvatarView>();
        private int _currentAvatarId;
        private IPopupService _popupController;
        private string _cachedPlayerName;

        [Inject]
        public void Construct(IPopupService popupService)
        {
            _popupController = popupService;
        }

        private void Start()
        {
            if (editNameButton != null)
            {
                editNameButton.onClick.RemoveAllListeners();
                editNameButton.onClick.AddListener(OnEditNameButtonClicked);
            }
        }

        public void ShowProfileData(string playerName, Sprite avatarSprite)
        {
            _cachedPlayerName = playerName;
            UpdatePlayerNameDisplay(playerName);
            if (avatarImage != null && avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
        }

        public void UpdatePlayerNameDisplay(string name)
        {
            if (playerNameText != null) playerNameText.text = name;
            _cachedPlayerName = name;
        }

        public void UpdateAvatarDisplay(int avatarId, Sprite avatarSprite)
        {
            _currentAvatarId = avatarId;
            if (avatarImage != null && avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }

            foreach (var item in _avatarItems)
            {
                if (item != null) item.SetSelected(item.GetAvatarId() == _currentAvatarId);
            }
        }

        public void PopulateAvatarGrid(List<AvatarDisplayData> avatars)
        {
            foreach (var item in _avatarItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _avatarItems.Clear();

            foreach (var avatar in avatars)
            {
                if (avatarItemPrefab != null && avatarGridContainer != null)
                {
                    AvatarView newItem = Instantiate(avatarItemPrefab, avatarGridContainer);
                    
                    bool isSelected = avatar.Id == _currentAvatarId;
                    // Truyền dữ liệu dạng primitive xuống AvatarView
                    newItem.Initialize(avatar.Id, avatar.Name, avatar.Sprite, avatar.IsUnlocked, isSelected, OnAvatarGridItemSelected);
                    _avatarItems.Add(newItem);
                }
            }
        }

        public void SetSaveButtonInteractable(bool interactable)
        {
            
        }

        private void OnAvatarGridItemSelected(int avatarId)
        {
            OnAvatarSelected?.Invoke(avatarId);
        }

        private void OnEditNameButtonClicked()
        {
            OnEditNameRequested?.Invoke();
            OpenInputPopupAsync().Forget(); 
        }

        private async UniTaskVoid OpenInputPopupAsync()
        {
            if (_popupController == null) return;

            await _popupController.ShowPopup("PopupProfileInputText");
            var inputTextPopup = _popupController.GetPopup<PopupProfileInputText>("PopupProfileInputText");
            if (inputTextPopup != null)
            {
                inputTextPopup.SetTittleText("Edit Player Name");
                inputTextPopup.SetInputText(_cachedPlayerName);
                inputTextPopup.SetContinueCallback(OnNameInputComplete);
            }
        }

        private void OnNameInputComplete(string newName)
        {
            OnTemporaryNameChanged?.Invoke(newName);
        }
    }
}