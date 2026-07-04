using System;
using System.Collections.Generic; // Thêm thư viện này để dùng Dictionary
using ChieChie.GamePass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class BonusMilestoneUIItem : MonoBehaviour
    {
        [SerializeField] private Button btnClaim;
        [SerializeField] private GameObject objLocked;
        [SerializeField] private GameObject objClaimed;
        [SerializeField] private Transform customBonusIconContainer;

        private int _bonusIndex;
        private Action<int> _onClaimBonusClicked;

        // Cache lưu trữ: Key là Prefab gốc, Value là Instance đã Instantiate trong Container
        private readonly Dictionary<GameObject, GameObject> _cachedIcons = new Dictionary<GameObject, GameObject>();
        private GameObject _currentActiveIcon;

        public void Setup(BonusMilestoneUIData data, Action<int> onClaimBonusClicked)
        {
            _bonusIndex = data.Index;
            _onClaimBonusClicked = onClaimBonusClicked;
            
            btnClaim.gameObject.SetActive(data.State == MilestoneState.ReadyToClaim);
            objLocked.SetActive(data.State == MilestoneState.Locked);
            objClaimed.SetActive(data.State == MilestoneState.Claimed);

            // --- Xử lý Bonus Custom Icon bằng Cache ---
            UpdateCustomIcon(data.BonusIcon, customBonusIconContainer);
        }

        private void UpdateCustomIcon(GameObject prefab, Transform container)
        {
            // Ẩn icon hiện tại trước
            if (_currentActiveIcon != null)
            {
                _currentActiveIcon.SetActive(false);
                _currentActiveIcon = null;
            }

            if (prefab == null) return;

            // Kiểm tra xem Prefab này đã từng được tạo trong container này chưa
            if (_cachedIcons.TryGetValue(prefab, out var instance))
            {
                if (instance != null)
                {
                    instance.SetActive(true);
                    _currentActiveIcon = instance;
                }
                else
                {
                    // Đề phòng trường hợp instance bị hủy bởi lý do nào đó bên ngoài
                    CreateNewIcon(prefab, container);
                }
            }
            else
            {
                CreateNewIcon(prefab, container);
            }
        }

        private void CreateNewIcon(GameObject prefab, Transform container)
        {
            var newInstance = Instantiate(prefab, container);
            _cachedIcons[prefab] = newInstance;
            _currentActiveIcon = newInstance;
        }

        private void Awake()
        {
            btnClaim.onClick.AddListener(() => _onClaimBonusClicked?.Invoke(_bonusIndex));
        }
    }
}