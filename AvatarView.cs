using System.Collections.Generic;
using ChieChie.Constracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
        public class AvatarView : MonoBehaviour, IAvatarView
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI avatarNameText;
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TextMeshProUGUI unlockConditionText;
        
        private int _avatarId;
        private bool _isUnlocked;
        private System.Action<int> _onSelectCallback;
        
        public void Initialize(int id, string name, Sprite avatarSprite, bool isUnlocked, bool isSelected, System.Action<int> onSelectCallback)
        {
            _avatarId = id;
            _isUnlocked = isUnlocked;
            _onSelectCallback = onSelectCallback;
       
            DisplayAvatar(id, name, avatarSprite, isSelected, isUnlocked);
        }
        
        public void DisplayAvatar(int id, string name, Sprite avatarSprite, bool isSelected, bool isUnlocked)
        {
            _avatarId = id;
            _isUnlocked = isUnlocked;

            if (avatarNameText != null)
            {
                avatarNameText.text = name;
            }
            
            if (avatarImage != null && avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
            
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isSelected);
            }
            
            SetAvatarLockState(id, isUnlocked);
            
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectButtonClicked);
                selectButton.interactable = _isUnlocked;
            }
        }

        public void UpdateAvatarList(List<int> avatarIds)
        {
            throw new System.NotImplementedException();
        }
      
        public void SetAvatarLockState(int avatarId, bool isUnlocked)
        {
            if (avatarId != _avatarId) return;
            
            _isUnlocked = isUnlocked;
            
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!_isUnlocked);
            }
            
            if (unlockConditionText != null)
            {
                unlockConditionText.gameObject.SetActive(!_isUnlocked);
            }
            
            if (selectButton != null)
            {
                selectButton.interactable = _isUnlocked;
            }
        }
        
        public int GetAvatarId()
        {
            return _avatarId;
        }
        
        public bool IsUnlocked()
        {
            return _isUnlocked;
        }
        
        public void SetSelected(bool isSelected)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isSelected);
            }
        }
        
        private void OnSelectButtonClicked()
        {
            if (_isUnlocked)
            {
                _onSelectCallback?.Invoke(_avatarId);
            }
        }
    }
    }
