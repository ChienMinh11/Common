using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class BadgeItemView : MonoBehaviour
    {
        [SerializeField] private Image badgeIcon;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectOverlay;
        [SerializeField] private TextMeshProUGUI badgeNameText;
        [SerializeField] private Button clickButton;

        private int _badgeId;
        private Action<int> _onSelectedCallback;

        public void Initialize(int id, string name, Sprite sprite, bool isUnlocked, bool isSelected, Action<int> onSelected)
        {
            _badgeId = id;
            _onSelectedCallback = onSelected;

            if (badgeNameText != null) badgeNameText.text = name;
            if (badgeIcon != null) badgeIcon.sprite = sprite;
            
            SetUnlocked(isUnlocked);
            SetSelected(isSelected);

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onSelectedCallback?.Invoke(_badgeId));
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectOverlay != null) selectOverlay.SetActive(isSelected);
        }

        public void SetUnlocked(bool isUnlocked)
        {
            if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);
        }

        public int GetBadgeId() => _badgeId;
    }
}