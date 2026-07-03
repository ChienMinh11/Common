using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Core;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class MilestoneUIItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtIndex;
        [SerializeField] private GameObject starIndex;
        [SerializeField] private GamePassExpSlider expSlider;
        
        [Header("Progress Highlight Connection")]
        [SerializeField] private MilestoneHighlight milestoneHighlight;
    
        [Header("Free Pass")]
        [SerializeField] private RewardSlotView freeRewardSlotView;
        [SerializeField] private Button btnClaimFree;
        [SerializeField] private DOTweenAnimation btnClaimFreeScale;
        [SerializeField] private GameObject objFreeLocked;
        [SerializeField] private GameObject objFreeClaimed;
        [SerializeField] private GameObject freeIconContainer;
        [SerializeField] private Transform customFreeIconContainer;

        [Header("Premium Pass")]
        [SerializeField] private RewardSlotView premiumRewardSlotView;
        [SerializeField] private Button btnClaimPremium;
        [SerializeField] private DOTweenAnimation btnClaimPremiumScale;
        [SerializeField] private GameObject objPremiumLocked;
        [SerializeField] private GameObject objPremiumClaimed;
        [SerializeField] private GameObject premiumIconContainer;
        [SerializeField] private Transform customPremiumIconContainer;

        private int _milestoneIndex;
        private Action<int, bool> _onClaimClicked;

        private readonly Dictionary<GameObject, GameObject> _cachedFreeIcons = new Dictionary<GameObject, GameObject>();
        private readonly Dictionary<GameObject, GameObject> _cachedPremiumIcons = new Dictionary<GameObject, GameObject>();
        
        private GameObject _currentActiveFreeIcon;
        private GameObject _currentActivePremiumIcon;
        public int MilestoneIndex => _milestoneIndex;

        public void Setup(MilestoneUIData data, Action<int, bool> onClaimClicked)
        {
            _milestoneIndex = data.Index; 
            _onClaimClicked = onClaimClicked;
            
            bool isZeroIndex = _milestoneIndex == 0;
            if (starIndex != null) starIndex.SetActive(isZeroIndex);
            if (txtIndex != null) txtIndex.gameObject.SetActive(!isZeroIndex);
            
            txtIndex.text = $"{_milestoneIndex}";

            UpdateCustomIcon(data.CustomIconFreePass, customFreeIconContainer, _cachedFreeIcons, ref _currentActiveFreeIcon);
            
            if (data.CustomIconFreePass != null)
            {
                if (freeIconContainer != null) freeIconContainer.SetActive(false);
            }
            else
            {
                if (freeIconContainer != null) freeIconContainer.SetActive(true);
                if (freeRewardSlotView != null && data.FreeRewards != null && data.FreeRewards.Count > 0)
                {
                    var freeReward = data.FreeRewards[0];
                    Sprite rewardSprite = freeReward.IsInfiniteReward ? freeReward.InfinityRewardIcon : freeReward.IconReward;
                    freeRewardSlotView.Setup(
                        freeReward.IsInfiniteReward, 
                        freeReward.Amount, 
                        freeReward.InfinityDuration, 
                        rewardSprite, 
                        showPrefix: true
                    );
                }
            }
            
            UpdateStateUI(data.FreeState, btnClaimFree, objFreeLocked, objFreeClaimed);

            UpdateCustomIcon(data.CustomIconPremiumPass, customPremiumIconContainer, _cachedPremiumIcons, ref _currentActivePremiumIcon);

            if (data.CustomIconPremiumPass != null)
            {
                if (premiumIconContainer != null) premiumIconContainer.SetActive(false);
            }
            else
            {
                if (premiumIconContainer != null) premiumIconContainer.SetActive(true); 
                if (premiumRewardSlotView != null && data.PremiumRewards != null && data.PremiumRewards.Count > 0)
                {
                    var premiumReward = data.PremiumRewards[0];
                    Sprite rewardSprite = premiumReward.IsInfiniteReward ? premiumReward.InfinityRewardIcon : premiumReward.IconReward;
                    premiumRewardSlotView.Setup(
                        premiumReward.IsInfiniteReward, 
                        premiumReward.Amount, 
                        premiumReward.InfinityDuration, 
                        rewardSprite, 
                        showPrefix: true
                    );
                }
            }

            UpdateStateUI(data.PremiumState, btnClaimPremium, objPremiumLocked, objPremiumClaimed);
        }

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
        
        public void UpdateHighlightState(int currentLevelIndex)
        {
            if (milestoneHighlight == null) return;

            if (_milestoneIndex == currentLevelIndex)
            {
                milestoneHighlight.SetImmediate(1f);
            }
            else 
            {
                milestoneHighlight.SetDefault();
            }
        }

        public void SetExpSliderProgress(float progress)
        {
            if (expSlider == null) return;
            expSlider.SetProgress(Mathf.Clamp01(progress), string.Empty);
        }

        public async UniTask PlayExpSliderAnimationAsync(float fromProgress, float toProgress, CancellationToken ct)
        {
            if (expSlider == null) return;

            await expSlider.PlaySliderAnimationAsync(
                Mathf.Clamp01(fromProgress),
                Mathf.Clamp01(toProgress),
                string.Empty,
                string.Empty,
                ct
            );
        }

        public async UniTask SetHighlightByAnimationAsync(float value, CancellationToken ct, float duration)
        {
            if (milestoneHighlight == null) return;

            ct.ThrowIfCancellationRequested();
            await milestoneHighlight.SetByAnimationAsync(value, duration);
            ct.ThrowIfCancellationRequested();
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