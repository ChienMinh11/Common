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
        [SerializeField] private Transform customBonusBankIconContainer;

        private Action _onClaimClicked;
        private readonly Dictionary<GameObject, GameObject> _cachedIcons = new Dictionary<GameObject, GameObject>();
        private GameObject _currentActiveIcon;

        private void Awake()
        {
            if (btnClaim != null) btnClaim.onClick.AddListener(() => _onClaimClicked?.Invoke());
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

            bool isClaimed = data.State == MilestoneState.Claimed;
            bool isReadyToClaim = data.State == MilestoneState.ReadyToClaim;
            bool showUnlocked = data.IsUnlocked || data.CurrentAmount > 0 || isReadyToClaim || isClaimed;

            if (lockContainer != null) lockContainer.SetActive(!showUnlocked);
            if (unLockContainer != null) unLockContainer.SetActive(showUnlocked);
            if (objLocked != null) objLocked.SetActive(!showUnlocked);
            if (objClaimed != null) objClaimed.SetActive(isClaimed);
            if (btnClaim != null) btnClaim.gameObject.SetActive(isReadyToClaim);

            if (amountSlider != null)
            {
                int currentAmount = Mathf.Clamp(data.CurrentAmount, 0, data.MaxAmount);
                float progress = data.MaxAmount > 0 ? (float)currentAmount / data.MaxAmount : 0f;
                amountSlider.SetProgress(progress, $"{currentAmount}/{data.MaxAmount}");
            }
          

            UpdateCustomIcon(data.BonusBankIcon);
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
        public async Cysharp.Threading.Tasks.UniTask PlayAmountAnimationAsync(BonusBankUIData fromData, BonusBankUIData toData, System.Threading.CancellationToken ct)
        {
            if (amountSlider == null || fromData == null || toData == null || !toData.IsAvailable) return;

            int fromAmount = Mathf.Clamp(fromData.CurrentAmount, 0, fromData.MaxAmount);
            int toAmount = Mathf.Clamp(toData.CurrentAmount, 0, toData.MaxAmount);

            float fromProgress = fromData.MaxAmount > 0 ? (float)fromAmount / fromData.MaxAmount : 0f;
            float toProgress = toData.MaxAmount > 0 ? (float)toAmount / toData.MaxAmount : 0f;

            // Chạy animation tăng chỉ số trên Slider của BonusBank
            await amountSlider.PlaySliderAnimationAsync(
                fromProgress,
                toProgress,
                $"{fromAmount}/{fromData.MaxAmount}",
                $"{toAmount}/{toData.MaxAmount}",
                ct
            );
        }
    }
    
   
}


