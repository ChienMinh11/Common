using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.GamePass;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class BonusBankMilestoneUiItem : MonoBehaviour
    {
        [SerializeField] private Button btnClaim;
        [SerializeField] private GameObject lockContainer;
        [SerializeField] private GameObject unLockContainer;
        [SerializeField] private GameObject objLocked;
        [SerializeField] private GameObject objClaimed;
        [SerializeField] private GamePassExpSlider amountSlider;
        [SerializeField] private Button btnShowRewardInfo;
        [SerializeField] private RewardSlotView rewardSlotView;
        [SerializeField] private Transform customBonusBankIconContainer;

        private Action _onClaimClicked;
        private Action<IReadOnlyList<IItemReward>, Transform> _onRewardInfoClicked;
        private IReadOnlyList<IItemReward> _rewards;
        private readonly Dictionary<GameObject, GameObject> _cachedIcons = new Dictionary<GameObject, GameObject>();
        private GameObject _currentActiveIcon;

        private void Awake()
        {
            if (btnClaim != null) btnClaim.onClick.AddListener(() => _onClaimClicked?.Invoke());
            if (btnShowRewardInfo != null) btnShowRewardInfo.onClick.AddListener(HandleRewardInfoClicked);
        }

        public void Setup(
            BonusBankUIData data,
            Action onClaimClicked,
            Action<IReadOnlyList<IItemReward>, Transform> onRewardInfoClicked = null)
        {
            if (data == null || !data.IsAvailable)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _onClaimClicked = onClaimClicked;
            _onRewardInfoClicked = onRewardInfoClicked;
            _rewards = data.Rewards;

            bool isClaimed = data.State == MilestoneState.Claimed;
            bool isReadyToClaim = data.State == MilestoneState.ReadyToClaim;
            bool showUnlocked = data.IsUnlocked || data.CurrentAmount > 0 || isReadyToClaim || isClaimed;

            if (lockContainer != null) lockContainer.SetActive(!showUnlocked);
            if (unLockContainer != null) unLockContainer.SetActive(showUnlocked);
            if (objLocked != null) objLocked.SetActive(!showUnlocked);
            if (objClaimed != null) objClaimed.SetActive(isClaimed);
            if (btnClaim != null) btnClaim.gameObject.SetActive(isReadyToClaim);
            if (btnShowRewardInfo != null) btnShowRewardInfo.gameObject.SetActive(_rewards != null && _rewards.Count > 0);

            if (amountSlider != null)
            {
                int currentAmount = Mathf.Clamp(data.CurrentAmount, 0, data.MaxAmount);
                float progress = data.MaxAmount > 0 ? (float)currentAmount / data.MaxAmount : 0f;
                amountSlider.SetProgress(progress, $"{currentAmount}/{data.MaxAmount}");
            }

            if (rewardSlotView != null && _rewards != null && _rewards.Count > 0)
            {
                rewardSlotView.Setup(_rewards[0]);
            }

            UpdateCustomIcon(data.BonusBankIcon);
        }

        public Transform GetRewardIconTransform()
        {
            if (_currentActiveIcon != null && _currentActiveIcon.activeSelf && customBonusBankIconContainer != null)
            {
                return customBonusBankIconContainer;
            }

            return rewardSlotView != null ? rewardSlotView.transform : transform;
        }

        private void HandleRewardInfoClicked()
        {
            if (_rewards == null || _rewards.Count == 0) return;
            _onRewardInfoClicked?.Invoke(_rewards, GetRewardIconTransform());
        }

        private void UpdateCustomIcon(GameObject prefab)
        {
            if (_currentActiveIcon != null)
            {
                _currentActiveIcon.SetActive(false);
                _currentActiveIcon = null;
            }

            if (prefab == null || customBonusBankIconContainer == null) return;

            if (_cachedIcons.TryGetValue(prefab, out var instance))
            {
                if (instance != null)
                {
                    instance.SetActive(true);
                    _currentActiveIcon = instance;
                    return;
                }

                _cachedIcons.Remove(prefab);
            }

            var newInstance = Instantiate(prefab, customBonusBankIconContainer);
            _cachedIcons[prefab] = newInstance;
            _currentActiveIcon = newInstance;
        }
    }
}
