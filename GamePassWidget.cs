using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class GamePassWidget : MonoBehaviour, IPassView
    {
        [SerializeField] private string viewId = nameof(GamePassWidget);

        [Header("UI Components")]
        [SerializeField] private GamePassExpSlider expSlider;
        [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
        
        [Header("Notification")]
        [SerializeField] private GameObject objNotificationBadge; 
        [SerializeField] private TMP_Text txtClaimableCount;
        private IPassService _passService;
        private PassViewData _lastViewData;
        private bool _playManualRefreshAnimation;
        private CancellationTokenSource _refreshAnimationCts;

        public event Action<int, bool> OnClaimRewardClicked;
        public event Action<int> OnClaimBonusClicked;
        public event Action OnBuyPremiumClicked;
        public string ViewId => string.IsNullOrEmpty(viewId) ? nameof(GamePassWidget) : viewId;

        [Inject]
        public void Constructor(IPassService passService)
        {
            _passService = passService;
            _passService.RegisterView(this); 
        }

        private void OnDestroy()
        {
            CancelRefreshAnimation();

            if (_passService != null)
            {
                _passService.UnregisterView(this);
            }
        }

        private void OnDisable()
        {
            CancelRefreshAnimation();
        }

        [Button]
        private void RefreshUIManual()
        {
            _playManualRefreshAnimation = true;
            _passService.FlushDelayedUIUpdate(this);
        }

        public void RefreshUI(PassViewData viewData)
        {
            if (viewData == null) return;
            if (viewData.EventEndTime == DateTime.MinValue || DateTime.UtcNow >= viewData.EventEndTime)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                _lastViewData = viewData;
                return;
            }
            else
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
            }
            if (timeCountdownWidget != null)
            {
                timeCountdownWidget.Setup(viewData.EventEndTime);
            }

            bool shouldAnimateManualRefresh = _playManualRefreshAnimation &&
                                              _lastViewData != null &&
                                              expSlider != null &&
                                              viewData.CurrentExp > _lastViewData.CurrentExp;
            _playManualRefreshAnimation = false;

            CancelRefreshAnimation();

            if (shouldAnimateManualRefresh)
            {
                var fromViewData = _lastViewData;
                _refreshAnimationCts = new CancellationTokenSource();
                AnimateManualRefreshAsync(fromViewData, viewData, _refreshAnimationCts.Token).Forget();
            }
            else
            {
                UpdateExpProgress(viewData);
                UpdateClaimableUI(viewData);
            }

            _lastViewData = viewData;
        }
        
        private void UpdateExpProgress(PassViewData viewData)
        {
            if (expSlider != null)
            {
                expSlider.UpdateProgressDetailed(viewData);
            }
        }

        private async UniTask AnimateManualRefreshAsync(PassViewData fromViewData, PassViewData toViewData, CancellationToken ct)
        {
            try
            {
                UpdateClaimableUI(toViewData, fromViewData.CurrentExp);

                foreach (var step in BuildExpAnimationSteps(toViewData, fromViewData.CurrentExp, toViewData.CurrentExp))
                {
                    ct.ThrowIfCancellationRequested();

                    expSlider.SetProgress(step.FromProgress, step.FromText);
                    await expSlider.PlaySliderAnimationAsync(step.FromProgress, step.ToProgress, step.FromText, ct);
                    expSlider.SetProgress(step.ToProgress, step.ToText);

                    UpdateClaimableUI(toViewData, step.EndExpForClaimable);
                }

                UpdateExpProgress(toViewData);
                UpdateClaimableUI(toViewData);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation when another refresh starts or this widget is disabled/destroyed.
            }
        }

        private IEnumerable<ExpAnimationStep> BuildExpAnimationSteps(PassViewData viewData, int fromExp, int toExp)
        {
            int currentExp = Mathf.Max(0, fromExp);
            int targetExp = Mathf.Max(currentExp, toExp);

            while (currentExp < targetExp)
            {
                ExpProgressState fromState = GetProgressState(viewData, currentExp);
                if (fromState.IsCompleted)
                {
                    yield return new ExpAnimationStep
                    {
                        FromProgress = fromState.Progress,
                        ToProgress = 1f,
                        FromText = fromState.Text,
                        ToText = fromState.Text,
                        EndExpForClaimable = targetExp
                    };
                    yield break;
                }

                int endExp = Mathf.Min(targetExp, fromState.LevelEndExp);
                bool reachedLevelEnd = endExp >= fromState.LevelEndExp;

                if (reachedLevelEnd)
                {
                    yield return new ExpAnimationStep
                    {
                        FromProgress = fromState.Progress,
                        ToProgress = 1f,
                        FromText = fromState.Text,
                        ToText = $"{fromState.RequiredExp}/{fromState.RequiredExp}",
                        EndExpForClaimable = endExp
                    };
                }
                else
                {
                    ExpProgressState toState = GetProgressState(viewData, endExp);
                    yield return new ExpAnimationStep
                    {
                        FromProgress = fromState.Progress,
                        ToProgress = toState.Progress,
                        FromText = fromState.Text,
                        ToText = toState.Text,
                        EndExpForClaimable = endExp
                    };
                }

                if (endExp <= currentExp) yield break;
                currentExp = endExp;
            }
        }

        private ExpProgressState GetProgressState(PassViewData viewData, int totalExp)
        {
            int accumulatedNormalExp = 0;
            var sortedMilestones = viewData.Milestones?.OrderBy(m => m.Index).ToList();

            if (sortedMilestones != null)
            {
                foreach (var milestone in sortedMilestones)
                {
                    int levelStartExp = accumulatedNormalExp;
                    int levelEndExp = accumulatedNormalExp + milestone.RequiredExp;
                    if (totalExp < levelEndExp)
                    {
                        int expInLevel = Mathf.Max(0, totalExp - levelStartExp);
                        float progress = milestone.RequiredExp > 0 ? (float)expInLevel / milestone.RequiredExp : 0f;
                        return new ExpProgressState
                        {
                            Progress = progress,
                            Text = $"{expInLevel}/{milestone.RequiredExp}",
                            LevelEndExp = levelEndExp,
                            RequiredExp = milestone.RequiredExp
                        };
                    }

                    accumulatedNormalExp = levelEndExp;
                }
            }

            var sortedBonus = viewData.BonusMilestones?.OrderBy(b => b.Index).ToList();
            if (sortedBonus == null || sortedBonus.Count == 0)
            {
                return ExpProgressState.Completed;
            }

            int bonusExp = Mathf.Max(0, totalExp - accumulatedNormalExp);
            int accumulatedBonusExpBefore = 0;
            foreach (var bonus in sortedBonus)
            {
                if (bonusExp < bonus.RequiredExp)
                {
                    int requiredExp = bonus.RequiredExp - accumulatedBonusExpBefore;
                    int expInBonus = Mathf.Max(0, bonusExp - accumulatedBonusExpBefore);
                    float progress = requiredExp > 0 ? (float)expInBonus / requiredExp : 0f;
                    return new ExpProgressState
                    {
                        Progress = progress,
                        Text = $"{expInBonus}/{requiredExp}",
                        LevelEndExp = accumulatedNormalExp + bonus.RequiredExp,
                        RequiredExp = requiredExp
                    };
                }

                accumulatedBonusExpBefore = bonus.RequiredExp;
            }

            return ExpProgressState.Completed;
        }

        private void UpdateClaimableUI(PassViewData viewData)
        {
            UpdateClaimableUI(viewData, viewData.CurrentExp);
        }

        private void UpdateClaimableUI(PassViewData viewData, int displayedExp)
        {
            int claimableCount = CalculateClaimableCount(viewData, displayedExp);

            if (objNotificationBadge != null)
            {
                objNotificationBadge.SetActive(claimableCount > 0);
            }

            if (txtClaimableCount != null)
            {
                txtClaimableCount.text = claimableCount.ToString();
            }
        }

        private int CalculateClaimableCount(PassViewData viewData, int displayedExp)
        {
            int claimableCount = 0;
            int accumulatedNormalExp = 0;

            if (viewData.Milestones != null)
            {
                foreach (var milestone in viewData.Milestones.OrderBy(m => m.Index))
                {
                    accumulatedNormalExp += milestone.RequiredExp;
                    if (displayedExp < accumulatedNormalExp) continue;

                    if (milestone.FreeState == MilestoneState.ReadyToClaim) claimableCount++;
                    if (viewData.IsPremiumUnlocked && milestone.PremiumState == MilestoneState.ReadyToClaim) claimableCount++;
                }
            }

            if (viewData.IsPremiumUnlocked && viewData.BonusMilestones != null)
            {
                int bonusExp = Mathf.Max(0, displayedExp - accumulatedNormalExp);
                foreach (var bonus in viewData.BonusMilestones.OrderBy(b => b.Index))
                {
                    if (bonusExp < bonus.RequiredExp) continue;
                    if (bonus.State == MilestoneState.ReadyToClaim) claimableCount++;
                }
            }

            return claimableCount;
        }

        private void CancelRefreshAnimation()
        {
            if (_refreshAnimationCts == null) return;

            _refreshAnimationCts.Cancel();
            _refreshAnimationCts.Dispose();
            _refreshAnimationCts = null;
        }

        private struct ExpAnimationStep
        {
            public float FromProgress { get; set; }
            public float ToProgress { get; set; }
            public string FromText { get; set; }
            public string ToText { get; set; }
            public int EndExpForClaimable { get; set; }
        }

        private struct ExpProgressState
        {
            public static ExpProgressState Completed => new ExpProgressState
            {
                Progress = 1f,
                Text = "Completed!",
                LevelEndExp = int.MaxValue,
                RequiredExp = 0,
                IsCompleted = true
            };

            public float Progress { get; set; }
            public string Text { get; set; }
            public int LevelEndExp { get; set; }
            public int RequiredExp { get; set; }
            public bool IsCompleted { get; set; }
        }
    }
}