using System;
using System.Linq;
using System.Threading;
using ChieChie.Core;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Game.GamePlay
{
    public class GamePassWidget : MonoBehaviour, IPassView,IWidgetIdentity
    {
        [SerializeField] private SideWidgetStructConfig config;
        [SerializeField] private string viewId = nameof(GamePassWidget);

        [Header("UI Components")]
        [SerializeField] private GamePassExpSlider expSlider;
        [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
        
        [Header("Notification")]
        [SerializeField] private GameObject objNotificationBadge; 
        [SerializeField] private TMP_Text txtClaimableCount;
        private IPassService _passService;
        private IEventService _eventService;
        private PassViewData _lastViewData;
        private bool _playManualRefreshAnimation;
        private CancellationTokenSource _refreshAnimationCts;

        public event Action<int, bool> OnClaimRewardClicked;
        public event Action<int> OnClaimBonusClicked;
        public event Action OnClaimBonusBankClicked;
        public event Action OnBuyPremiumClicked;
        public string ViewId => string.IsNullOrEmpty(viewId) ? nameof(GamePassWidget) : viewId;
        public SideWidgetStructConfig Config => config;
      

        [Inject]
        public void Constructor(IPassService passService,IEventService eventService)
        {
            _passService = passService;
            _eventService = eventService;
           
        }
        
        public void Initialize()
        {
            gameObject.SetActive(true);
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
                _eventService.PublishEvent(WidgetEventType.OnWidgetStateChanged);
                _lastViewData = viewData;
                return;
            }
            else
            {
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                _eventService.PublishEvent(WidgetEventType.OnWidgetStateChanged);
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
                var animationSteps = EventProgressAnimationCalculator.CalculateAnimationSteps(toViewData,
                    fromViewData.CurrentExp,
                    toViewData.CurrentExp);

                int stepStartExp = fromViewData.CurrentExp;
                foreach (var step in animationSteps)
                {
                    ct.ThrowIfCancellationRequested();

                    string fromProgressText = GetAnimationProgressText(toViewData, stepStartExp, stepStartExp, step.FromProgressText);
                    string toProgressText = GetAnimationProgressText(toViewData, stepStartExp, step.EvaluatedExpForClaimableCheck, step.ToProgressText);

                    expSlider.SetProgress(step.FromProgressPercentage, fromProgressText);
                    await expSlider.PlaySliderAnimationAsync(
                        step.FromProgressPercentage, 
                        step.ToProgressPercentage, 
                        fromProgressText,
                        toProgressText,
                        ct
                    );
                    
                    expSlider.SetProgress(step.ToProgressPercentage, toProgressText);
                    UpdateClaimableUI(toViewData, step.EvaluatedExpForClaimableCheck);
                    stepStartExp = step.EvaluatedExpForClaimableCheck;
                }

                UpdateExpProgress(toViewData);
                UpdateClaimableUI(toViewData);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
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

            if (viewData.BonusBank != null && viewData.BonusBank.State == MilestoneState.ReadyToClaim)
            {
                int bonusExp = Mathf.Max(0, displayedExp - accumulatedNormalExp);
                int displayedAmount = viewData.BonusBank.ConvertBonusExpToAmount(bonusExp);

                if (displayedAmount >= viewData.BonusBank.MaxAmount)
                {
                    claimableCount++;
                }
            }

            return claimableCount;
        }

        private static string GetAnimationProgressText(PassViewData viewData, int stepStartExp, int totalExp, string fallbackText)
        {
            if (!GamePassExpSlider.TryGetBonusBankProgressTextAtExp(viewData, stepStartExp, out _))
            {
                return fallbackText;
            }

            return GamePassExpSlider.TryGetBonusBankProgressTextAtExp(viewData, totalExp, out string bonusBankText)
                ? bonusBankText
                : fallbackText;
        }

        private void CancelRefreshAnimation()
        {
            if (_refreshAnimationCts == null) return;

            _refreshAnimationCts.Cancel();
            _refreshAnimationCts.Dispose();
            _refreshAnimationCts = null;
        }


       
    }
}
