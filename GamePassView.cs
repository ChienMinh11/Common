using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.Core;
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
        private const string COMPLETED = "Completed!";
        [SerializeField] private string viewId = nameof(GamePassView);
        [Header("Top Bar")] 
        [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
        [SerializeField] private TMP_Text txtCurrentExp;
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

        

        public void Initialize(IPassService passService)
        {
            _passService = passService;
            _passService.RegisterView(this);
        }

        private void Awake()
        {
            btnBuyPremium.onClick.AddListener(() => OnBuyPremiumClicked?.Invoke());
        }

        private void OnDestroy()
        {
            _passService.UnregisterView(this);
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

            if (timeCountdownWidget != null)
            {
                timeCountdownWidget.Setup(viewData.EventEndTime);
            }
            UpdateExpProgressUI(viewData);
            btnBuyPremium.gameObject.SetActive(!viewData.IsPremiumUnlocked);
            if (objPremiumBadge != null)
            {
                objPremiumBadge.SetActive(viewData.IsPremiumUnlocked);
            }
            
            // --- Tái cấu trúc logic cập nhật EXP tiến trình & chạy slider ---
            bool shouldAnimateManualRefresh = _playManualRefreshAnimation &&
                                              _lastViewData != null &&
                                              expSlider != null &&
                                              viewData.CurrentExp > _lastViewData.CurrentExp;
            _playManualRefreshAnimation = false;

            CancelRefreshAnimation();

            if (shouldAnimateManualRefresh)
            {
                _refreshAnimationCts = new CancellationTokenSource();
                AnimateManualRefreshAsync(_lastViewData, viewData, _refreshAnimationCts.Token).Forget();
            }
            else
            {
                UpdateExpProgressUI(viewData);
                UpdateLevelText(viewData, viewData.CurrentExp);
            }

            if (viewData.Milestones != null)
            {
                var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();

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
                var sortedBonus = viewData.BonusMilestones.OrderBy(b => b.Index).ToList(); //

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
            _lastViewData = viewData;
        }
        private async UniTask AnimateManualRefreshAsync(PassViewData fromViewData, PassViewData toViewData, CancellationToken ct)
        {
            try
            {
                var animationSteps = EventProgressAnimationCalculator.CalculateAnimationSteps(toViewData, fromViewData.CurrentExp, toViewData.CurrentExp);

                UpdateLevelText(toViewData, fromViewData.CurrentExp);

                foreach (var step in animationSteps)
                {
                    ct.ThrowIfCancellationRequested();
                    expSlider.SetProgress(step.FromProgressPercentage, step.FromProgressText);
                    await expSlider.PlaySliderAnimationAsync(step.FromProgressPercentage, step.ToProgressPercentage, step.FromProgressText, ct);
                    expSlider.SetProgress(step.ToProgressPercentage, step.ToProgressText);
                    UpdateLevelText(toViewData, step.EvaluatedExpForClaimableCheck);
                }
       
                UpdateExpProgressUI(toViewData);
                UpdateLevelText(toViewData, toViewData.CurrentExp);
            }
            catch (OperationCanceledException)
            {
                // Bị hủy khi tắt UI hoặc có đợt refresh mới chồng lên
            }
        }
        private void UpdateLevelText(PassViewData viewData, int currentExp)
        {
            if (txtCurrentLevel == null) return;

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

            int maxMilestoneIndex = viewData.Milestones != null && viewData.Milestones.Count > 0 
                ? viewData.Milestones.Max(m => m.Index) 
                : calculatedMilestoneIndex;

            int nextMilestoneIndex = Mathf.Min(calculatedMilestoneIndex + 1, maxMilestoneIndex);
            txtCurrentLevel.text = $"{nextMilestoneIndex}";
        }

        private void UpdateExpProgressUI(PassViewData viewData)
        {
            if (expSlider == null) return;
            bool isBonusActive = expSlider.UpdateProgressDetailed(viewData);
            if (txtCurrentLevel != null) txtCurrentLevel.gameObject.SetActive(!isBonusActive);
            if (objBonusPassActiveVisual != null) objBonusPassActiveVisual.SetActive(isBonusActive);
        }

        private void HandleClaimRewardItem(int index, bool isPremium)
        {
            OnClaimRewardClicked?.Invoke(index, isPremium);
        }

        private void HandleClaimBonusItem(int index)
        {
            OnClaimBonusClicked?.Invoke(index);
        }
        private void CancelRefreshAnimation()
        {
            if (_refreshAnimationCts == null) return;
            _refreshAnimationCts.Cancel();
            _refreshAnimationCts.Dispose();
            _refreshAnimationCts = null;
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
    }
}
