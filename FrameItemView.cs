using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class FrameItemView : MonoBehaviour
    {
        [SerializeField] private Image frameIcon;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectOverlay;
        [SerializeField] private TextMeshProUGUI frameNameText;
        [SerializeField] private Button clickButton;

        private int _frameId;
        private Action<int> _onSelectedCallback;

        public void Initialize(int id, string name, Sprite sprite, bool isUnlocked, bool isSelected, Action<int> onSelected)
        {
            _frameId = id;
            _onSelectedCallback = onSelected;

            if (frameNameText != null) frameNameText.text = name;
            if (frameIcon != null) frameIcon.sprite = sprite;
            
            SetUnlocked(isUnlocked);
            SetSelected(isSelected);

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onSelectedCallback?.Invoke(_frameId));
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

        public int GetFrameId() => _frameId;
    }
}