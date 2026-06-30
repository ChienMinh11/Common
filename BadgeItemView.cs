using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class BadgeItemView : MonoBehaviour
    {
        [SerializeField] private Image badgeIcon; // Hiển thị ảnh tĩnh khi unselect
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectOverlay;
        [SerializeField] private TextMeshProUGUI badgeNameText;
        [SerializeField] private Button clickButton;

        // THÊM MỚI: Container trên UI của ô Grid để chứa Prefab hiệu ứng/Spine khi được chọn
        [SerializeField] private Transform fxContainer;

        private int _badgeId;
        private GameObject _badgePrefab;
        private Action<int> _onSelectedCallback;

        // Pool nội bộ của riêng ô Grid này để lưu đối tượng hiệu ứng động sau khi sinh
        private GameObject _pooledFxInstance;

        public void Initialize(int id, string name, Sprite sprite, GameObject prefab, bool isUnlocked, bool isSelected, Action<int> onSelected)
        {
            _badgeId = id;
            _badgePrefab = prefab; // Lưu lại reference của Prefab hiệu ứng
            _onSelectedCallback = onSelected;

            if (badgeNameText != null) badgeNameText.text = name;
            if (badgeIcon != null) badgeIcon.sprite = sprite;
            
            SetUnlocked(isUnlocked);
            SetSelected(isSelected); // Hàm này sẽ điều khiển bật/tắt cả Fx động công nghệ Pool

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onSelectedCallback?.Invoke(_badgeId));
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectOverlay != null) selectOverlay.SetActive(isSelected);

            // XỬ LÝ POOL CHỐNG SPAM CLICK:
            if (isSelected)
            {
                // Khi được chọn: Ẩn tạm thời Icon tĩnh đi để nhường chỗ cho hiệu ứng Prefab động hiển thị
                if (badgeIcon != null) badgeIcon.enabled = false;

                if (_badgePrefab != null && fxContainer != null)
                {
                    // Nếu chưa từng sinh hiệu ứng này trước đây (Pool trống tại ô này)
                    if (_pooledFxInstance == null)
                    {
                        _pooledFxInstance = Instantiate(_badgePrefab, fxContainer);
                        _pooledFxInstance.transform.localPosition = Vector3.zero;
                        _pooledFxInstance.transform.localRotation = Quaternion.identity;
                        _pooledFxInstance.transform.localScale = Vector3.one;
                    }
                    
                    // Chỉ kích hoạt lại đối tượng cũ chứ KHÔNG Instantiate mới mỗi lần chọn
                    _pooledFxInstance.SetActive(true);
                }
            }
            else
            {
                // Khi bỏ chọn (Unselect): Hiện lại ảnh Icon tĩnh thông thường
                if (badgeIcon != null) badgeIcon.enabled = true;

                // Chỉ ẩn hiệu ứng động đi, giữ lại instance trong bộ nhớ Pool của ô này
                if (_pooledFxInstance != null)
                {
                    _pooledFxInstance.SetActive(false);
                }
            }
        }

        public void SetUnlocked(bool isUnlocked)
        {
            if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);
        }

        public int GetBadgeId() => _badgeId;

        private void OnDestroy()
        {
            // Giải phóng bộ nhớ Pool nội bộ khi danh sách Grid bị Clear hoặc View bị hủy
            if (_pooledFxInstance != null)
            {
                Destroy(_pooledFxInstance);
            }
        }
    }
}