using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
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

        [Header("Progress Highlight Connection")] [SerializeField]
        private MilestoneHighlight milestoneHighlight;

        [Header("Free Pass")] 
        [SerializeField] private RewardSlotView freeRewardSlotView;
        [SerializeField] private Button btnClaimFree;
        [SerializeField] private Button btnShowFreeRewardInfo;
        [SerializeField] private GameObject objFreeLocked;
        [SerializeField] private GameObject objFreeClaimed;
        [SerializeField] private GameObject freeIconContainer;
        [SerializeField] private Transform customFreeIconContainer;

        [Header("Premium Pass")] [SerializeField]
        private RewardSlotView premiumRewardSlotView;

        [SerializeField] private Button btnClaimPremium;
        [SerializeField] private Button btnShowPremiumRewardInfo;
        [SerializeField] private GameObject objPremiumLocked;
        [SerializeField] private GameObject objPremiumClaimed;
        [SerializeField] private GameObject premiumIconContainer;
        [SerializeField] private Transform customPremiumIconContainer;

        private int _milestoneIndex;
        private Action<int, bool> _onClaimClicked;
        private Action<IReadOnlyList<IItemReward>, Transform, MilestoneState, bool> _onRewardInfoClicked;
        private IReadOnlyList<IItemReward> _freeRewards;
        private IReadOnlyList<IItemReward> _premiumRewards;
        private MilestoneState _freeState;
        private MilestoneState _premiumState;
        private bool _showClaimButtons = true;

        private readonly Dictionary<GameObject, GameObject> _cachedFreeIcons = new Dictionary<GameObject, GameObject>();

        private readonly Dictionary<GameObject, GameObject> _cachedPremiumIcons =
            new Dictionary<GameObject, GameObject>();

        private GameObject _currentActiveFreeIcon;
        private GameObject _currentActivePremiumIcon;
        public int MilestoneIndex => _milestoneIndex;
        
        private void Awake()
        {
            if (btnClaimFree != null) btnClaimFree.onClick.AddListener(() => _onClaimClicked?.Invoke(_milestoneIndex, false));
            if (btnClaimPremium != null) btnClaimPremium.onClick.AddListener(() => _onClaimClicked?.Invoke(_milestoneIndex, true));
            if (btnShowFreeRewardInfo != null) 
                btnShowFreeRewardInfo.onClick.AddListener(OnFreeRewardInfoClicked);
        
            if (btnShowPremiumRewardInfo != null) 
                btnShowPremiumRewardInfo.onClick.AddListener(OnPremiumRewardInfoClicked);
        }
        private void OnFreeRewardInfoClicked() => HandleRewardInfoClicked(false);
        private void OnPremiumRewardInfoClicked() => HandleRewardInfoClicked(true);

        public void Setup(
            MilestoneUIData data,
            Action<int, bool> onClaimClicked,
            bool showClaimButtons = true,
            Action<IReadOnlyList<IItemReward>, Transform, MilestoneState, bool> onRewardInfoClicked = null)
        {
            _milestoneIndex = data.Index;
            _onClaimClicked = onClaimClicked;
            _onRewardInfoClicked = onRewardInfoClicked;
            _freeRewards = data.FreeRewards;
            _premiumRewards = data.PremiumRewards;
            _freeState = data.FreeState;
            _premiumState = data.PremiumState;
            _showClaimButtons = showClaimButtons;

            bool isZeroIndex = _milestoneIndex == 0;
            if (starIndex != null) starIndex.SetActive(isZeroIndex);
            if (txtIndex != null) txtIndex.gameObject.SetActive(!isZeroIndex);

            txtIndex.text = $"{_milestoneIndex}";

            UpdateCustomIcon(data.CustomIconFreePass, customFreeIconContainer, _cachedFreeIcons,
                ref _currentActiveFreeIcon);

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
                    freeRewardSlotView.Setup(freeReward);
                }
            }

            UpdateStateUI(_freeState, btnClaimFree, objFreeLocked, objFreeClaimed);
            UpdateRewardInfoButton(btnShowFreeRewardInfo, _freeRewards);

            UpdateCustomIcon(data.CustomIconPremiumPass, customPremiumIconContainer, _cachedPremiumIcons,
                ref _currentActivePremiumIcon);

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
                    premiumRewardSlotView.Setup(premiumReward);
                }
            }

            UpdateStateUI(_premiumState, btnClaimPremium, objPremiumLocked, objPremiumClaimed);
            UpdateRewardInfoButton(btnShowPremiumRewardInfo, _premiumRewards);
        }

        private void UpdateCustomIcon(GameObject prefab, Transform container, Dictionary<GameObject, GameObject> cache,
            ref GameObject currentActiveIcon)
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

        public void SetClaimButtonsVisible(bool visible)
        {
            _showClaimButtons = visible;
            UpdateStateUI(_freeState, btnClaimFree, objFreeLocked, objFreeClaimed);
            UpdateStateUI(_premiumState, btnClaimPremium, objPremiumLocked, objPremiumClaimed);
        }

        public void SetExpSliderProgress(float progress)
        {
            if (expSlider == null) return;
            expSlider.SetProgress(Mathf.Clamp01(progress), string.Empty);
        }

        public async UniTask PlayExpSliderAnimationAsync(float fromProgress, float toProgress, CancellationToken ct,
            float durationOverride = -1f)
        {
            if (expSlider == null) return;

            await expSlider.PlaySliderAnimationAsync(
                Mathf.Clamp01(fromProgress),
                Mathf.Clamp01(toProgress),
                string.Empty,
                string.Empty,
                ct,
                durationOverride
            );
        }

        public async UniTask SetHighlightByAnimationAsync(float value, CancellationToken ct, float duration)
        {
            if (milestoneHighlight == null) return;

            ct.ThrowIfCancellationRequested();
            await milestoneHighlight.SetByAnimationAsync(value, duration);
            ct.ThrowIfCancellationRequested();
        }
        public Transform GetRewardIconTransform(bool isPremium)
        {
            if (isPremium)
            {
                Transform fallback = premiumRewardSlotView != null ? premiumRewardSlotView.transform : transform;
                return _currentActivePremiumIcon != null && _currentActivePremiumIcon.activeSelf && customPremiumIconContainer != null
                    ? customPremiumIconContainer
                    : fallback;
            }
            else
            {
                Transform fallback = freeRewardSlotView != null ? freeRewardSlotView.transform : transform;
                return _currentActiveFreeIcon != null && _currentActiveFreeIcon.activeSelf && customFreeIconContainer != null
                    ? customFreeIconContainer
                    : fallback;
            }
        }

        private void UpdateStateUI(MilestoneState state, Button btn, GameObject lockObj, GameObject claimedObj)
        {
            if (btn != null) btn.gameObject.SetActive(_showClaimButtons && state == MilestoneState.ReadyToClaim);
            if (lockObj != null) lockObj.SetActive(state == MilestoneState.Locked);
            if (claimedObj != null) claimedObj.SetActive(state == MilestoneState.Claimed);
        }

        private void UpdateRewardInfoButton(Button button, IReadOnlyList<IItemReward> rewards)
        {
            if (button == null) return;
            button.gameObject.SetActive(rewards != null && rewards.Count > 0);
        }

        private void HandleRewardInfoClicked(bool isPremium)
        {
            IReadOnlyList<IItemReward> rewards = isPremium ? _premiumRewards : _freeRewards;
            if (rewards == null || rewards.Count == 0) return;

            MilestoneState state = isPremium ? _premiumState : _freeState;
            _onRewardInfoClicked?.Invoke(rewards, GetRewardIconTransform(isPremium), state, isPremium);
        }
        

       
    }
}
