using System;
using System.Collections.Generic; // Thêm thư viện này để dùng Dictionary
using ChieChie.Core;
using ChieChie.GamePass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class MilestoneUIItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtIndex;
        [SerializeField] private GameObject starIndex;
    
        [Header("Free Pass")]
        [SerializeField] private RewardSlotView freeRewardSlotView;
        [SerializeField] private Image imgFreeIcon;
        [SerializeField] private TMP_Text txtFreeAmount;
        [SerializeField] private Button btnClaimFree;
        [SerializeField] private GameObject objFreeLocked;
        [SerializeField] private GameObject objFreeClaimed;
        [SerializeField] private GameObject freeIconContainer;
        [SerializeField] private Transform customFreeIconContainer;

        [Header("Premium Pass")]
        [SerializeField] private RewardSlotView premiumRewardSlotView;
        [SerializeField] private Image imgPremiumIcon;
        [SerializeField] private TMP_Text txtPremiumAmount;
        [SerializeField] private Button btnClaimPremium;
        [SerializeField] private GameObject objPremiumLocked;
        [SerializeField] private GameObject objPremiumClaimed;
        [SerializeField] private GameObject premiumIconContainer;
        [SerializeField] private Transform customPremiumIconContainer;

        private int _milestoneIndex;
        private Action<int, bool> _onClaimClicked;

        // Cache lưu trữ cho cả Free và Premium Pass
        private readonly Dictionary<GameObject, GameObject> _cachedFreeIcons = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<GameObject, GameObject> _cachedPremiumIcons = new Dictionary<GameObject, GameObject>();
        
        private GameObject _currentActiveFreeIcon;
        private GameObject _currentActivePremiumIcon;

        public void Setup(MilestoneUIData data, Action<int, bool> onClaimClicked)
        {
            _milestoneIndex = data.Index; 
            _onClaimClicked = onClaimClicked;
            
            bool isZeroIndex = _milestoneIndex == 0;
            if (starIndex != null) starIndex.SetActive(isZeroIndex);
            if (txtIndex != null) txtIndex.gameObject.SetActive(!isZeroIndex);
            
            txtIndex.text = $"{_milestoneIndex}";

            // --- Xử lý Free Pass Icon bằng Cache ---
            UpdateCustomIcon(data.CustomIconFreePass, customFreeIconContainer, _cachedFreeIcons, ref _currentActiveFreeIcon);
            
            if (data.CustomIconFreePass != null)
            {
                freeIconContainer.SetActive(false);
            }
            else
            {
                freeIconContainer.SetActive(true);
                if (data.FreeRewards != null && data.FreeRewards.Count > 0)
                {
                    var freeReward = data.FreeRewards[0];
                    imgFreeIcon.sprite = freeReward.IsInfiniteReward ? freeReward.InfinityRewardIcon : freeReward.IconReward;
                }
            }
            
            if (data.FreeRewards != null && data.FreeRewards.Count > 0)
            {
                var freeReward = data.FreeRewards[0];
                txtFreeAmount.text = freeReward.IsInfiniteReward ? CoreExtensions.FormatTime(freeReward.InfinityDuration) : freeReward.Amount.ToString();
            }
            UpdateStateUI(data.FreeState, btnClaimFree, objFreeLocked, objFreeClaimed);

            // --- Xử lý Premium Pass Icon bằng Cache ---
            UpdateCustomIcon(data.CustomIconPremiumPass, customPremiumIconContainer, _cachedPremiumIcons, ref _currentActivePremiumIcon);

            if (data.CustomIconPremiumPass != null)
            {
                premiumIconContainer.SetActive(false);
            }
            else
            {
                premiumIconContainer.SetActive(true); 
                if (data.PremiumRewards != null && data.PremiumRewards.Count > 0)
                {
                    var premiumReward = data.PremiumRewards[0];
                    imgPremiumIcon.sprite = premiumReward.IsInfiniteReward ? premiumReward.InfinityRewardIcon : premiumReward.IconReward;
                }
            }

            if (data.PremiumRewards != null && data.PremiumRewards.Count > 0)
            {
                var premiumReward = data.PremiumRewards[0];
                txtPremiumAmount.text = premiumReward.IsInfiniteReward ? CoreExtensions.FormatTime(premiumReward.InfinityDuration) : premiumReward.Amount.ToString();
            }
            UpdateStateUI(data.PremiumState, btnClaimPremium, objPremiumLocked, objPremiumClaimed);
        }

        // Hàm dùng chung tối ưu hóa việc quản lý và cập nhật Custom Icon tránh Instantiate liên tục
        private void UpdateCustomIcon(GameObject prefab, Transform container, Dictionary<GameObject, GameObject> cache, ref GameObject currentActiveIcon)
        {
            if (currentActiveIcon != null)
            {
                currentActiveIcon.SetActive(false);
                currentActiveIcon = null;
            }

            if (prefab == null) return;

            if (cache.TryGetValue(prefab, out var instance))
            {
                if (instance != null)
                {
                    instance.SetActive(true);
                    currentActiveIcon = instance;
                }
                else
                {
                    instance = Instantiate(prefab, container);
                    cache[prefab] = instance;
                    currentActiveIcon = instance;
                }
            }
            else
            {
                var newInstance = Instantiate(prefab, container);
                cache[prefab] = newInstance;
                currentActiveIcon = newInstance;
            }
        }

        private void UpdateStateUI(MilestoneState state, Button btn, GameObject lockObj, GameObject claimedObj)
        {
            btn.gameObject.SetActive(state == MilestoneState.ReadyToClaim);
            lockObj.SetActive(state == MilestoneState.Locked); 
            claimedObj.SetActive(state == MilestoneState.Claimed); 
        }

        private void Awake()
        {
            btnClaimFree.onClick.AddListener(() => _onClaimClicked?.Invoke(_milestoneIndex, false));
            btnClaimPremium.onClick.AddListener(() => _onClaimClicked?.Invoke(_milestoneIndex, true));
        }
    }
}