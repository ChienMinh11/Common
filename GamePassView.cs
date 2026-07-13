using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Core.Utilities;
using ChieChie.GamePass;
using ChieChie.MVP;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class GamePassView : BaseView<PassPresenter>, IPassView
    {
        [SerializeField] private string viewId = nameof(GamePassView);
        [Header("Top Bar")] [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
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

        [Header("Bonus Milestones List")] [SerializeField]
        private Transform bonusContainer;

        [SerializeField] private BonusMilestoneUIItem bonusPrefab;

        [Header("Bonus Bank Milestone")] [SerializeField]
        private BonusBankMilestoneUiItem _bonusBankMilestoneUiItem;

        [Header("Reward Info")] [SerializeField]
        private ItemRewardInfoPanel rewardInfoPanel;

        [Header("Flow Events")] public UnityEvent OnFlowStarted;
        public UnityEvent OnFlowEnded;

        private readonly List<MilestoneUIItem> _milestonePool = new List<MilestoneUIItem>();
        private readonly List<BonusMilestoneUIItem> _bonusPool = new List<BonusMilestoneUIItem>();

        public event Action<int, bool> OnClaimRewardClicked;
        public event Action<int> OnClaimBonusClicked;
        public event Action OnClaimBonusBankClicked;
        public event Action OnBuyPremiumClicked;

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

        private IPassService _passService;
        private IEventService _eventService;
        public string ViewId => string.IsNullOrEmpty(viewId) ? nameof(GamePassView) : viewId;
        private PassViewData _lastViewData;
        private bool _playManualRefreshAnimation;
        private CancellationTokenSource _refreshAnimationCts;
        private CancellationTokenSource _scrollCts;

        public void Initialize(PassModel passModel, IEventService eventService)
        {
            _passService = passModel;
            _eventService = eventService;

            if (passModel != null && Presenter == null)
            {
                BindPresenter(new PassPresenter(this, passModel));
            }
        }

        private void Awake()
        {
            btnBuyPremium.onClick.AddListener(() => OnBuyPremiumClicked?.Invoke());
        }

        private void OnEnable()
        {
         
        }

        public void RefreshUIManual()
        {
            _playManualRefreshAnimation = true;
            Presenter?.FlushDelayedUIUpdate();
        }

        public void RefreshUI(PassViewData viewData)
        {
            if (viewData == null) return;

            if (timeCountdownWidget != null)
            {
                timeCountdownWidget.Setup(viewData.EventEndTime);
            }

            int maxMilestoneIndex = GetMaxMilestoneIndex(viewData);
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
                bool showClaimButtons = !shouldAnimateManualRefresh;

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

                    item.Setup(sortedMilestones[i], HandleClaimRewardItem, showClaimButtons, HandleRewardInfoRequest);
                    if (highlightedMilestoneIndex < GetMaxMilestoneIndex(viewData))
                    {
                    }

                    if (item.MilestoneIndex == maxMilestoneIndex)
                    {
                        item.UpdateHighlightState(-1);
                    }
                    else
                    {
                        item.UpdateHighlightState(highlightedMilestoneIndex);
                    }

                    item.SetExpSliderProgress(GetCompletedMilestoneSliderProgress(initialViewData,
                        initialViewData.CurrentExp, item.MilestoneIndex));
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

                    item.Setup(sortedBonus[i], HandleClaimBonusItem, HandleRewardInfoRequest);
                }

                for (int i = sortedBonus.Count; i < _bonusPool.Count; i++)
                {
                    _bonusPool[i].gameObject.SetActive(false);
                }
            }

            if (_bonusBankMilestoneUiItem != null)
            {
                if (viewData.BonusBank != null && viewData.BonusBank.IsAvailable)
                {
                    var bankDataToSetup = shouldAnimateManualRefresh ? _lastViewData.BonusBank : viewData.BonusBank;
                    _bonusBankMilestoneUiItem.Setup(bankDataToSetup, HandleClaimBonusBankItem, HandleRewardInfoRequest);
                }
                else
                {
                    _bonusBankMilestoneUiItem.gameObject.SetActive(false);
                }
            }

            if (shouldAnimateManualRefresh)
            {
                _refreshAnimationCts = new CancellationTokenSource();
                AnimateManualRefreshAsync(_lastViewData, viewData, _refreshAnimationCts.Token).Forget();
            }

            _lastViewData = viewData;
        }

        public void RunScrollFirstTimeOpenPopup()
        {
            if (_passService != null && !_passService.IsFirstOpen)
            {
                HandleFirstOpenScrollAsync().Forget();
            }
            else
            {
                ScrollToMilestone(_lastViewData.CurrentMilestoneIndex, animate: false).Forget();
            }
        }

        private async UniTask AnimateManualRefreshAsync(PassViewData fromViewData, PassViewData toViewData,
            CancellationToken ct)
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
                SetClaimButtonsVisible(false);
                OnStartFlow();

                int topSliderAnimatedExp = fromViewData.CurrentExp;
                int stepStartExp = fromViewData.CurrentExp;
                foreach (var step in animationSteps)
                {
                    ct.ThrowIfCancellationRequested();

                    string fromProgressText =
                        GetAnimationProgressText(toViewData, stepStartExp, stepStartExp, step.FromProgressText);
                    string toProgressText = GetAnimationProgressText(toViewData, stepStartExp,
                        step.EvaluatedExpForClaimableCheck, step.ToProgressText);

                    await expSlider.PlaySliderAnimationAsync(
                        step.FromProgressPercentage,
                        step.ToProgressPercentage,
                        fromProgressText,
                        toProgressText,
                        ct
                    );

                    topSliderAnimatedExp = step.EvaluatedExpForClaimableCheck;
                    stepStartExp = topSliderAnimatedExp;
                    UpdateLevelText(toViewData, topSliderAnimatedExp);
                }

                UpdateExpProgressUI(toViewData);
                UpdateLevelText(toViewData, toViewData.CurrentExp);

                await AnimateMilestoneHighlightAsync(fromMilestoneIndex, 0f, ct, 0.5f);

                await PlayContinuousMilestoneFlowAsync(fromViewData, toViewData, nextMilestoneIndex, ct);
                if (_bonusBankMilestoneUiItem != null && fromViewData.BonusBank != null && toViewData.BonusBank != null)
                {
                    await _bonusBankMilestoneUiItem.PlayAmountAnimationAsync(fromViewData.BonusBank,
                        toViewData.BonusBank, ct);
                    _bonusBankMilestoneUiItem.Setup(toViewData.BonusBank, HandleClaimBonusBankItem,
                        HandleRewardInfoRequest);
                }

                SetCompletedMilestoneSlidersByExp(toViewData, toViewData.CurrentExp);
                if (nextMilestoneIndex < GetMaxMilestoneIndex(toViewData))
                {
                    await AnimateMilestoneHighlightAsync(nextMilestoneIndex, 1f, ct, 0.5f);
                }

                UpdateMilestoneHighlightState(nextMilestoneIndex);
                SetClaimButtonsVisible(true);
                OnEndFlow();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask PlayContinuousMilestoneFlowAsync(PassViewData fromViewData, PassViewData toViewData,
            int nextMilestoneIndex, CancellationToken ct)
        {
            var completedMilestoneIndices =
                GetCompletedMilestoneIndicesBetween(fromViewData.CurrentExp, toViewData.CurrentExp, toViewData);

            if (completedMilestoneIndices.Count == 0)
            {
                await ScrollToMilestone(nextMilestoneIndex, animate: true, ct: ct);
                return;
            }

            float itemDuration = GetMilestoneFlowItemDuration(completedMilestoneIndices.Count);

            await PlayCompletedMilestoneFlowItemsAsync(completedMilestoneIndices, itemDuration, ct);

            int lastCompletedMilestoneIndex = completedMilestoneIndices[completedMilestoneIndices.Count - 1];
            if (nextMilestoneIndex != lastCompletedMilestoneIndex)
            {
                await ScrollToMilestone(nextMilestoneIndex, animate: true, duration: itemDuration, ct: ct);
            }
        }

        private async UniTask PlayCompletedMilestoneFlowItemsAsync(List<int> completedMilestoneIndices,
            float itemDuration, CancellationToken ct)
        {
            foreach (int milestoneIndex in completedMilestoneIndices)
            {
                ct.ThrowIfCancellationRequested();

                var item = GetMilestoneItem(milestoneIndex);
                UniTask scrollTask = ScrollToMilestone(milestoneIndex, animate: true, duration: itemDuration, ct: ct);
                if (item == null)
                {
                    await scrollTask;
                    continue;
                }

                UniTask sliderTask = item.PlayExpSliderAnimationAsync(0f, 1f, ct, itemDuration);
                await UniTask.WhenAll(scrollTask, sliderTask);
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

        private static string GetAnimationProgressText(PassViewData viewData, int stepStartExp, int totalExp,
            string fallbackText)
        {
            if (!GamePassExpSlider.TryGetBonusBankProgressTextAtExp(viewData, stepStartExp, out _))
            {
                return fallbackText;
            }

            return GamePassExpSlider.TryGetBonusBankProgressTextAtExp(viewData, totalExp, out string bonusBankText)
                ? bonusBankText
                : fallbackText;
        }

        private static float GetCompletedMilestoneSliderProgress(PassViewData viewData, int currentExp,
            int milestoneIndex)
        {
            int completedMilestoneIndex = GetCompletedMilestoneIndex(viewData, currentExp);
            return milestoneIndex <= completedMilestoneIndex ? 1f : 0f;
        }

        private void SetCompletedMilestoneSlidersByExp(PassViewData viewData, int currentExp)
        {
            foreach (var item in _milestonePool)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                item.SetExpSliderProgress(
                    GetCompletedMilestoneSliderProgress(viewData, currentExp, item.MilestoneIndex));
            }
        }

        private void SetClaimButtonsVisible(bool visible)
        {
            int currentMilestoneIndex = GetCompletedMilestoneIndex(_lastViewData, _lastViewData.CurrentExp);
            int maxMilestoneIndex = GetMaxMilestoneIndex(_lastViewData);
            foreach (var item in _milestonePool)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                if (item.MilestoneIndex < currentMilestoneIndex || currentMilestoneIndex >= maxMilestoneIndex)
                {
                    item.SetClaimButtonsVisible(true);
                }
                else
                {
                    // Chỉ những mốc đang ở hiện tại hoặc tương lai mới bật/tắt theo hiệu ứng
                    item.SetClaimButtonsVisible(visible);
                }
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
            int maxMilestoneIndex = GetMaxMilestoneIndex(_lastViewData);

            foreach (var item in _milestonePool)
            {
                if (item == null || !item.gameObject.activeSelf) continue;


                if (item.MilestoneIndex == maxMilestoneIndex)
                {
                    item.UpdateHighlightState(-1);
                }
                else
                {
                    item.UpdateHighlightState(milestoneIndex);
                }
            }
        }

        private MilestoneUIItem GetMilestoneItem(int milestoneIndex)
        {
            return _milestonePool.FirstOrDefault(item =>
                item != null && item.gameObject.activeSelf && item.MilestoneIndex == milestoneIndex);
        }

        private async UniTask AnimateMilestoneHighlightAsync(int milestoneIndex, float value, CancellationToken ct,
            float duration)
        {
            var targetItem = GetMilestoneItem(milestoneIndex);
            if (targetItem == null) return;
            await targetItem.SetHighlightByAnimationAsync(value, ct, duration);
        }

        private void HandleClaimRewardItem(int index, bool isPremium)
        {
            var item = GetMilestoneItem(index);
            Transform spawnTransform = item != null ? item.GetRewardIconTransform(isPremium) : this.transform;

            PublishClaimedRewards(GetClaimableMilestoneRewards(index, isPremium), spawnTransform);
            OnClaimRewardClicked?.Invoke(index, isPremium);
        }

        private void HandleClaimBonusItem(int index)
        {
            OnClaimBonusClicked?.Invoke(index);
        }

        private void HandleClaimBonusBankItem()
        {
            OnClaimBonusBankClicked?.Invoke();
        }

        private void HandleRewardInfoRequest(IReadOnlyList<IItemReward> rewards, Transform anchor)
        {
            if (rewardInfoPanel == null) return;
            rewardInfoPanel.ShowRewards(rewards, anchor);
        }

        private void HandleRewardInfoRequest(IReadOnlyList<IItemReward> rewards, Transform anchor, MilestoneState state,
            bool isPremium)
        {
            if (rewardInfoPanel == null) return;
            rewardInfoPanel.ShowRewards(rewards, anchor, GetRewardDescriptionState(state, isPremium));
        }

        private static ItemRewardInfoPanel.RewardDescriptionState GetRewardDescriptionState(MilestoneState state,
            bool isPremium)
        {
            if (state == MilestoneState.Claimed)
            {
                return ItemRewardInfoPanel.RewardDescriptionState.Claimed;
            }

            if (state == MilestoneState.Locked && isPremium)
            {
                return ItemRewardInfoPanel.RewardDescriptionState.PremiumLocked;
            }

            return ItemRewardInfoPanel.RewardDescriptionState.Unclaimed;
        }

        private int GetMaxMilestoneIndex(PassViewData viewData)
        {
            if (viewData?.Milestones == null || viewData.Milestones.Count == 0)
                return -1;

            return viewData.Milestones.Max(m => m.Index);
        }

        private List<IItemReward> GetClaimableMilestoneRewards(int index, bool isPremium)
        {
            var milestone = _lastViewData?.Milestones?.FirstOrDefault(m => m.Index == index);
            if (milestone == null) return null;

            MilestoneState state = isPremium ? milestone.PremiumState : milestone.FreeState;
            if (state != MilestoneState.ReadyToClaim) return null;

            return isPremium ? milestone.PremiumRewards : milestone.FreeRewards;
        }

        private List<IItemReward> GetClaimableBonusRewards(int index)
        {
            var bonusMilestone = _lastViewData?.BonusMilestones?.FirstOrDefault(b => b.Index == index);
            if (bonusMilestone == null || bonusMilestone.State != MilestoneState.ReadyToClaim) return null;

            return bonusMilestone.Rewards;
        }


        private void PublishClaimedRewards(IEnumerable<IItemReward> rewards, Transform spawnTransform)
        {
            if (_eventService == null || rewards == null) return;

            var rewardEventData = RewardItemDataHelper.FromItemRewards(rewards, true, true);
            if (rewardEventData.Count == 0) return;

            var args = new RewardClaimedArgs
            {
                Rewards = rewardEventData,
                SpawnTransform = spawnTransform
            };

            _eventService.Publish(GameEvent.OnRewardClaimByGamePass, args);
        }

        private async UniTask ScrollToMilestone(int milestoneIndex, bool animate = false, float duration = 0.5f,
            CancellationToken ct = default(CancellationToken))
        {
            if (milestoneScrollRect == null) return;

            CancelScrollAnimation();

            var targetItem = GetMilestoneItem(milestoneIndex);

            if (targetItem != null)
            {
                var rectTransform = targetItem.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    if (!animate)
                    {
                        ForceRebuildMilestoneScrollLayout();
                        milestoneScrollRect.FocusOnItem(rectTransform, bias: 0.5f);
                        return;
                    }

                    if (!milestoneScrollRect.gameObject.activeInHierarchy || !gameObject.activeInHierarchy)
                    {
                        milestoneScrollRect.FocusOnItem(rectTransform, bias: 0.5f);
                        return;
                    }

                    _scrollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    try
                    {
                        float finalDuration = animate ? duration : 0f;
                        await milestoneScrollRect.FocusOnItemAsync(rectTransform, finalDuration, _scrollCts.Token,
                            bias: 0.5f);
                    }
                    catch (System.OperationCanceledException)
                    {
                    }
                }
            }
        }
        private async UniTaskVoid HandleFirstOpenScrollAsync()
        {
            if (milestoneScrollRect == null) return;

            var token = this.destroyCancellationToken;
            CancelScrollAnimation();

            try
            {
                OnStartFlow();
                if (endIndex != null && endIndex is RectTransform endRect)
                {
                    ForceRebuildMilestoneScrollLayout();
                    milestoneScrollRect.FocusOnItem(endRect, bias: 0.5f);
                }
                else if (_milestonePool.Count > 0)
                {
                    var lastItem = _milestonePool.LastOrDefault(item => item != null && item.gameObject.activeSelf);
                    if (lastItem != null)
                    {
                        ForceRebuildMilestoneScrollLayout();
                        milestoneScrollRect.FocusOnItem(lastItem.GetComponent<RectTransform>(), bias: 0.5f);
                    }
                }

                Vector2 startNormalizedPos = milestoneScrollRect.normalizedPosition;
                Vector2 targetNormalizedPos = Vector2.zero;

                if (startIndex != null && startIndex is RectTransform startRect)
                {
                    milestoneScrollRect.FocusOnItem(startRect, bias: 0.5f);
                    targetNormalizedPos = milestoneScrollRect.normalizedPosition;
                    milestoneScrollRect.normalizedPosition = startNormalizedPos;
                }
                await UniTask.Delay(1000,false, PlayerLoopTiming.Update, token);
                float duration = 3f; 
                await milestoneScrollRect.DONormalizedPos(targetNormalizedPos, duration)
                    .SetEase(Ease.OutQuad) 
                    .SetUpdate(UpdateType.Normal,
                        isIndependentUpdate: false) 
                    .WithCancellation(token);

            
                _passService.MarkFirstOpenCompleted();
                OnEndFlow();
                _eventService.PublishEvent(GameEvent.OnGamePassViewShowPopupTutorial);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) {}
        }

        private void ForceRebuildMilestoneScrollLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (milestoneContainer is RectTransform milestoneContainerRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(milestoneContainerRect);
            }

            if (milestoneScrollRect != null && milestoneScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(milestoneScrollRect.content);
            }

            Canvas.ForceUpdateCanvases();
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

        public void OnStartFlow()
        {
            OnFlowStarted?.Invoke();
        }

        public void OnEndFlow()
        {
            OnFlowEnded?.Invoke();
        }

        private void OnDisable()
        {
            if (rewardInfoPanel != null)
            {
                rewardInfoPanel.Hide();
            }

            if (_lastViewData != null)
            {
                ScrollToMilestone(_lastViewData.CurrentMilestoneIndex, animate: false).Forget();
            }

            CancelRefreshAnimation();
            CancelScrollAnimation();
        }

        public void OnShowTutorialPopupByClick()
        {
            _eventService.PublishEvent(GameEvent.OnGamePassViewShowPopupTutorial);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CancelRefreshAnimation();
            CancelScrollAnimation();
        }
    }
}
