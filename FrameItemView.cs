using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class FrameItemView : MonoBehaviour
    {
        [SerializeField] private Image frameIcon; // Dùng để hiển thị ảnh tĩnh
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectOverlay;
        [SerializeField] private TextMeshProUGUI frameNameText;
        [SerializeField] private Button clickButton;

        // THÊM MỚI: Container để chứa Prefab hiệu ứng khi ô này được chọn
        [SerializeField] private Transform fxContainer;

        private int _frameId;
        private GameObject _framePrefab;
        private Action<int> _onSelectedCallback;
        
        // Biến Pool nội bộ của riêng ô Grid này để lưu đối tượng hiệu ứng động
        private GameObject _pooledFxInstance;

        public void Initialize(int id, string name, Sprite sprite, GameObject prefab, bool isUnlocked, bool isSelected, Action<int> onSelected)
        {
            _frameId = id;
            _framePrefab = prefab; // Lưu lại reference của Prefab để sinh khi cần
            _onSelectedCallback = onSelected;

            if (frameNameText != null) frameNameText.text = name;
            if (frameIcon != null) frameIcon.sprite = sprite;
            
            SetUnlocked(isUnlocked);
            SetSelected(isSelected); // Hàm này giờ sẽ kiêm luôn việc bật/tắt Fx động

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onSelectedCallback?.Invoke(_frameId));
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectOverlay != null) selectOverlay.SetActive(isSelected);

            // XỬ LÝ POOL CHỐNG SPAM CHO ITEM GRID:
            if (isSelected)
            {
                // Khi được chọn: Ẩn Icon tĩnh đi để nhường chỗ cho hiệu ứng Prefab động (nếu muốn)
                if (frameIcon != null) frameIcon.enabled = false;

                if (_framePrefab != null && fxContainer != null)
                {
                    // Nếu chưa từng sinh hiệu ứng này trước đây (Pool trống)
                    if (_pooledFxInstance == null)
                    {
                        _pooledFxInstance = Instantiate(_framePrefab, fxContainer);
                        _pooledFxInstance.transform.localPosition = Vector3.zero;
                        _pooledFxInstance.transform.localRotation = Quaternion.identity;
                        _pooledFxInstance.transform.localScale = Vector3.one;
                    }
                    
                    // Bật hiệu ứng lên (Không Instantiate lại nếu đã có sẵn)
                    _pooledFxInstance.SetActive(true);
                }
            }
            else
            {
                // Khi bỏ chọn (Unselect): Hiện lại Icon tĩnh
                if (frameIcon != null) frameIcon.enabled = true;

                // Chỉ ẩn hiệu ứng đi chứ KHÔNG hủy (Destroy), lưu lại trong Pool nội bộ của ô này
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

        public int GetFrameId() => _frameId;

        private void OnDestroy()
        {
            // Dọn dẹp đối tượng trong Pool khi ô Grid này bị hủy (khi ClearGrid)
            if (_pooledFxInstance != null)
            {
                Destroy(_pooledFxInstance);
            }
        }
    }
}