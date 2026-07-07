using System;
using System.Collections.Generic; // Thêm thư viện này để dùng Dictionary
using ChieChie.Constracts;
using ChieChie.GamePass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class BonusMilestoneUIItem : MonoBehaviour
    {
        [SerializeField] private Button btnClaim;
        [SerializeField] private Button btnShowRewardInfo;
        [SerializeField] private GameObject objLocked;
        [SerializeField] private GameObject objClaimed;
        [SerializeField] private Transform customBonusIconContainer;

        private int _bonusIndex;
        private Action<int> _onClaimBonusClicked;
        private Action<IReadOnlyList<IItemReward>, Transform> _onRewardInfoClicked;
        private IReadOnlyList<IItemReward> _rewards;

        // Cache lưu trữ: Key là Prefab gốc, Value là Instance đã Instantiate trong Container
        private readonly Dictionary<GameObject, GameObject> _cachedIcons = new Dictionary<GameObject, GameObject>();
        private GameObject _currentActiveIcon;

        public void Setup(
            BonusMilestoneUIData data,
            Action<int> onClaimBonusClicked,
            Action<IReadOnlyList<IItemReward>, Transform> onRewardInfoClicked = null)
        {
            _bonusIndex = data.Index;
            _onClaimBonusClicked = onClaimBonusClicked;
            _onRewardInfoClicked = onRewardInfoClicked;
            _rewards = data.Rewards;
            
            if (btnClaim != null) btnClaim.gameObject.SetActive(data.State == MilestoneState.ReadyToClaim);
            if (btnShowRewardInfo != null) btnShowRewardInfo.gameObject.SetActive(_rewards != null && _rewards.Count > 0);
            if (objLocked != null) objLocked.SetActive(data.State == MilestoneState.Locked);
            if (objClaimed != null) objClaimed.SetActive(data.State == MilestoneState.Claimed);

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
            if (btnClaim != null) btnClaim.onClick.AddListener(() => _onClaimBonusClicked?.Invoke(_bonusIndex));
            if (btnShowRewardInfo != null) btnShowRewardInfo.onClick.AddListener(HandleRewardInfoClicked);
        }

        private void HandleRewardInfoClicked()
        {
            if (_rewards == null || _rewards.Count == 0) return;

            Transform anchor = _currentActiveIcon != null && _currentActiveIcon.activeSelf && customBonusIconContainer != null
                ? customBonusIconContainer
                : transform;
            _onRewardInfoClicked?.Invoke(_rewards, anchor);
        }
    }
}
