using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Core.Utilities;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class GamePassView : MonoBehaviour, IPassView 
    {
        [SerializeField] private string viewId = nameof(GamePassView);
        [Header("Top Bar")] 
        [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
        [SerializeField] private TMP_Text txtCurrentLevel;
        [SerializeField] private GamePassExpSlider expSlider;
        [SerializeField] private Button btnBuyPremium;
        [SerializeField] private GameObject objPremiumBadge;
        [SerializeField] private GameObject objBonusPassActiveVisual;
       

        [Header("Milestones List")] [SerializeField]
        private Transform milestoneContainer;
        [SerializeField] private Transform startIndex;
        [SerializeField] private Transform endIndex;
        [SerializeField] private ScrollRect milestoneScrollRect;
        [SerializeField] private float milestoneFlowMinItemDuration = 0.25f;
        [SerializeField] private float milestoneFlowMaxItemDuration = 0.65f;
        [SerializeField] private float milestoneFlowMaxTotalDuration = 4.25f;

        [SerializeField] private MilestoneUIItem milestonePrefab;

        [Header("Bonus Milestones List")] 
        [SerializeField] private Transform bonusContainer;
        [SerializeField] private BonusMilestoneUIItem bonusPrefab;

        private readonly List<MilestoneUIItem> _milestonePool = new List<MilestoneUIItem>();
        private readonly List<BonusMilestoneUIItem> _bonusPool = new List<BonusMilestoneUIItem>();

        public event Action<int, bool> OnClaimRewardClicked;
        public event Action<int> OnClaimBonusClicked;
        public event Action OnBuyPremiumClicked;

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

        private IPassService _passService;
        public string ViewId => string.IsNullOrEmpty(viewId) ? nameof(GamePassView) : viewId;
        private PassViewData _lastViewData;
        private bool _playManualRefreshAnimation;
        private CancellationTokenSource _refreshAnimationCts;
        private CancellationTokenSource _scrollCts;

        public void Initialize(IPassService passService)
        {
            _passService = passService;
            _passService.RegisterView(this);
        }

        private void Awake()
        {
            btnBuyPremium.onClick.AddListener(() => OnBuyPremiumClicked?.Invoke());
        }

        public void RefreshUIManual()
        {
            _playManualRefreshAnimation = true;
            _passService.FlushDelayedUIUpdate(this);
        }

        public void RefreshUI(PassViewData viewData)
        {
            if (viewData == null) return;

            if (timeCountdownWidget != null)
            {
                timeCountdownWidget.Setup(viewData.EventEndTime);
            }

            bool shouldAnimateManualRefresh = _playManualRefreshAnimation &&
                                              _lastViewData != null &&
                                              expSlider != null &&
                                              viewData.CurrentExp > _lastViewData.CurrentExp;
            PassViewData initialViewData = shouldAnimateManualRefresh ? _lastViewData : viewData;

            UpdateExpProgressUI(initialViewData);
            btnBuyPremium.gameObject.SetActive(!viewData.IsPremiumUnlocked);
            if (objPremiumBadge != null)
            {
                objPremiumBadge.SetActive(viewData.IsPremiumUnlocked);
            }

            _playManualRefreshAnimation = false;

            CancelRefreshAnimation();

            if (!shouldAnimateManualRefresh)
            {
                UpdateLevelText(viewData, viewData.CurrentExp);
            }

            if (viewData.Milestones != null)
            {
                var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();
                int highlightedMilestoneIndex = GetNextMilestoneIndex(viewData, initialViewData.CurrentExp);

                for (int i = 0; i < sortedMilestones.Count; i++)
                {
                    MilestoneUIItem item;
                    if (i < _milestonePool.Count)
                    {
                        item = _milestonePool[i];
                        item.gameObject.SetActive(true);
                    }
                    else
                    {
                        item = Instantiate(milestonePrefab, milestoneContainer);
                        _milestonePool.Add(item);
                    }
        
                    if (startIndex != null)
                    {
                        item.transform.SetSiblingIndex(startIndex.GetSiblingIndex() + i + 1);
                    }

                    item.Setup(sortedMilestones[i], HandleClaimRewardItem);
                    item.UpdateHighlightState(highlightedMilestoneIndex);
                    item.SetExpSliderProgress(GetCompletedMilestoneSliderProgress(initialViewData, initialViewData.CurrentExp, item.MilestoneIndex));
                }

                if (endIndex != null && startIndex != null)
                {
                    int nextIndexAfterMilestones = startIndex.GetSiblingIndex() + sortedMilestones.Count + 1;
                    endIndex.SetSiblingIndex(nextIndexAfterMilestones);
                }

                for (int i = sortedMilestones.Count; i < _milestonePool.Count; i++)
                {
                    _milestonePool[i].gameObject.SetActive(false);
                }
            }

            if (viewData.BonusMilestones != null)
            {
                var sortedBonus = viewData.BonusMilestones.OrderBy(b => b.Index).ToList();

                for (int i = 0; i < sortedBonus.Count; i++)
                {
                    BonusMilestoneUIItem item;
                    if (i < _bonusPool.Count)
                    {
                        item = _bonusPool[i];
                        item.gameObject.SetActive(true);
                    }
                    else
                    {
                        item = Instantiate(bonusPrefab, bonusContainer);
                        _bonusPool.Add(item);
                    }

                    item.Setup(sortedBonus[i], HandleClaimBonusItem);
                }

                for (int i = sortedBonus.Count; i < _bonusPool.Count; i++)
                {
                    _bonusPool[i].gameObject.SetActive(false);
                }
            }

            if (shouldAnimateManualRefresh)
            {
                _refreshAnimationCts = new CancellationTokenSource();
                AnimateManualRefreshAsync(_lastViewData, viewData, _refreshAnimationCts.Token).Forget();
            }
            else
            {
                ScrollToMilestone(viewData.CurrentMilestoneIndex, animate: false).Forget();
            }

            _lastViewData = viewData;
        }

        private async UniTask AnimateManualRefreshAsync(PassViewData fromViewData, PassViewData toViewData, CancellationToken ct)
        {
            try
            {
                var animationSteps = EventProgressAnimationCalculator
                    .CalculateAnimationSteps(toViewData, fromViewData.CurrentExp, toViewData.CurrentExp)
                    .ToList();

                int fromMilestoneIndex = GetNextMilestoneIndex(toViewData, fromViewData.CurrentExp);
                int nextMilestoneIndex = GetNextMilestoneIndex(toViewData, toViewData.CurrentExp);

                UpdateLevelText(toViewData, fromViewData.CurrentExp);
                SetCompletedMilestoneSlidersByExp(toViewData, fromViewData.CurrentExp);

                int topSliderAnimatedExp = fromViewData.CurrentExp;
                foreach (var step in animationSteps)
                {
                    ct.ThrowIfCancellationRequested();

                    await expSlider.PlaySliderAnimationAsync(
                        step.FromProgressPercentage,
                        step.ToProgressPercentage,
                        step.FromProgressText,
                        step.ToProgressText,
                        ct
                    );

                    topSliderAnimatedExp = step.EvaluatedExpForClaimableCheck;
                    UpdateLevelText(toViewData, topSliderAnimatedExp);
                }

                UpdateExpProgressUI(toViewData);
                UpdateLevelText(toViewData, toViewData.CurrentExp);

                await AnimateMilestoneHighlightAsync(fromMilestoneIndex, 0f, ct, 0.5f);

                await PlayContinuousMilestoneFlowAsync(fromViewData, toViewData, nextMilestoneIndex, ct);

                SetCompletedMilestoneSlidersByExp(toViewData, toViewData.CurrentExp);
                await AnimateMilestoneHighlightAsync(nextMilestoneIndex, 1f, ct, 0.5f);
                UpdateMilestoneHighlightState(nextMilestoneIndex);
            }
            catch (OperationCanceledException)
            {
              
            }
        }

        private async UniTask PlayContinuousMilestoneFlowAsync(PassViewData fromViewData, PassViewData toViewData, int nextMilestoneIndex, CancellationToken ct)
        {
            var completedMilestoneIndices = GetCompletedMilestoneIndicesBetween(fromViewData.CurrentExp, toViewData.CurrentExp, toViewData);

            if (completedMilestoneIndices.Count == 0)
            {
                await ScrollToMilestone(nextMilestoneIndex, animate: true, ct: ct);
                return;
            }

            float itemDuration = GetMilestoneFlowItemDuration(completedMilestoneIndices.Count);
            float totalDuration = itemDuration * completedMilestoneIndices.Count;

            UniTask scrollTask = ScrollToMilestone(nextMilestoneIndex, animate: true, duration: totalDuration, ct: ct);
            UniTask sliderTask = PlayCompletedMilestoneSlidersAsync(completedMilestoneIndices, itemDuration, ct);

            await UniTask.WhenAll(scrollTask, sliderTask);
        }

        private async UniTask PlayCompletedMilestoneSlidersAsync(List<int> completedMilestoneIndices, float itemDuration, CancellationToken ct)
        {
            foreach (int milestoneIndex in completedMilestoneIndices)
            {
                ct.ThrowIfCancellationRequested();

                var item = GetMilestoneItem(milestoneIndex);
                if (item == null) continue;

                await item.PlayExpSliderAnimationAsync(0f, 1f, ct, itemDuration);
                item.SetExpSliderProgress(1f);
            }
        }

        private float GetMilestoneFlowItemDuration(int completedCount)
        {
            if (completedCount <= 0) return milestoneFlowMaxItemDuration;

            float durationByMaxTotal = milestoneFlowMaxTotalDuration / completedCount;
            return Mathf.Clamp(durationByMaxTotal, milestoneFlowMinItemDuration, milestoneFlowMaxItemDuration);
        }

        private List<int> GetCompletedMilestoneIndicesBetween(int fromExp, int toExp, PassViewData viewData)
        {
            int completedBefore = GetCompletedMilestoneIndex(viewData, fromExp);
            int completedAfter = GetCompletedMilestoneIndex(viewData, toExp);

            if (completedAfter <= completedBefore) return new List<int>();

            return viewData.Milestones
                .OrderBy(m => m.Index)
                .Where(m => m.Index > completedBefore && m.Index <= completedAfter)
                .Select(m => m.Index)
                .ToList();
        }

        private void UpdateLevelText(PassViewData viewData, int currentExp)
        {
            if (txtCurrentLevel == null) return;

            var nextMilestoneIndex = GetNextMilestoneIndex(viewData, currentExp);
            txtCurrentLevel.text = $"{nextMilestoneIndex}";
        }

        private static int GetNextMilestoneIndex(PassViewData viewData, int currentExp)
        {
            int calculatedMilestoneIndex = GetCompletedMilestoneIndex(viewData, currentExp);
            int maxMilestoneIndex = viewData.Milestones != null && viewData.Milestones.Count > 0 
                ? viewData.Milestones.Max(m => m.Index) 
                : calculatedMilestoneIndex;

            int nextMilestoneIndex = Mathf.Min(calculatedMilestoneIndex + 1, maxMilestoneIndex);
            return nextMilestoneIndex;
        }

        private static int GetCompletedMilestoneIndex(PassViewData viewData, int currentExp)
        {
            int calculatedMilestoneIndex = 0;
            if (viewData.Milestones != null && viewData.Milestones.Count > 0)
            {
                var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();
                int tempExp = currentExp;
                foreach (var milestone in sortedMilestones)
                {
                    if (tempExp >= milestone.RequiredExp)
                    {
                        calculatedMilestoneIndex = milestone.Index;
                        tempExp -= milestone.RequiredExp;
                    }
                    else break;
                }
            }

            return calculatedMilestoneIndex;
        }

        private static float GetCompletedMilestoneSliderProgress(PassViewData viewData, int currentExp, int milestoneIndex)
        {
            int completedMilestoneIndex = GetCompletedMilestoneIndex(viewData, currentExp);
            return milestoneIndex <= completedMilestoneIndex ? 1f : 0f;
        }

        private void SetCompletedMilestoneSlidersByExp(PassViewData viewData, int currentExp)
        {
            foreach (var item in _milestonePool)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                item.SetExpSliderProgress(GetCompletedMilestoneSliderProgress(viewData, currentExp, item.MilestoneIndex));
            }
        }

        private void UpdateExpProgressUI(PassViewData viewData)
        {
            if (expSlider == null) return;
            bool isBonusActive = expSlider.UpdateProgressDetailed(viewData);
            if (txtCurrentLevel != null) txtCurrentLevel.gameObject.SetActive(!isBonusActive);
            if (objBonusPassActiveVisual != null) objBonusPassActiveVisual.SetActive(isBonusActive);
        }

        private void UpdateMilestoneHighlightState(int milestoneIndex)
        {
            foreach (var item in _milestonePool)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                item.UpdateHighlightState(milestoneIndex);
            }
        }

        private MilestoneUIItem GetMilestoneItem(int milestoneIndex)
        {
            return _milestonePool.FirstOrDefault(item => item != null && item.gameObject.activeSelf && item.MilestoneIndex == milestoneIndex);
        }

        private async UniTask AnimateMilestoneHighlightAsync(int milestoneIndex, float value, CancellationToken ct, float duration)
        {
            var targetItem = GetMilestoneItem(milestoneIndex);
            if (targetItem == null) return;

            await targetItem.SetHighlightByAnimationAsync(value, ct, duration);
        }

        private void HandleClaimRewardItem(int index, bool isPremium)
        {
            OnClaimRewardClicked?.Invoke(index, isPremium);
        }

        private void HandleClaimBonusItem(int index)
        {
            OnClaimBonusClicked?.Invoke(index);
        }

        private async UniTask ScrollToMilestone(int milestoneIndex, bool animate = false, float duration = 0.5f, CancellationToken ct = default(CancellationToken))
        {
            if (milestoneScrollRect == null) return;

            CancelScrollAnimation();

            var targetItem = GetMilestoneItem(milestoneIndex);

            if (targetItem != null)
            {
                var rectTransform = targetItem.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    if (!milestoneScrollRect.gameObject.activeInHierarchy || !gameObject.activeInHierarchy)
                    {
                        milestoneScrollRect.FocusOnItem(rectTransform, bias: 0.5f);
                        return;
                    }

                    _scrollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    try
                    {
                        float finalDuration = animate ? duration : 0f;
                        await milestoneScrollRect.FocusOnItemAsync(rectTransform, finalDuration, _scrollCts.Token, bias: 0.5f);
                    }
                    catch (System.OperationCanceledException)
                    {
                       
                    }
                }
            }
        }

        private void CancelRefreshAnimation()
        {
            if (_refreshAnimationCts == null) return;
            _refreshAnimationCts.Cancel();
            _refreshAnimationCts.Dispose();
            _refreshAnimationCts = null;
        }
        
        private void CancelScrollAnimation()
        {
            if (_scrollCts != null)
            {
                _scrollCts.Cancel();
                _scrollCts.Dispose();
                _scrollCts = null;
            }
        }

        private void ClearOldItems()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null && item.transform != startIndex) 
                {
                    Destroy(item);
                }
            }
            _spawnedItems.Clear();
        }

        private void OnDisable()
        {
            CancelRefreshAnimation();
            CancelScrollAnimation();
        }

        private void OnDestroy()
        {
            if (_passService != null)
            {
                _passService.UnregisterView(this);
            }
            CancelRefreshAnimation();
            CancelScrollAnimation();
        }
    }
}
