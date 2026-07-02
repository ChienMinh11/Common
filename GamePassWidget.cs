using System;
using System.Linq;
using ChieChie.GamePass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class GamePassWidget : MonoBehaviour, IPassView
    {
        [Header("UI Components")]
        [SerializeField] private Slider sliderExpProgress;
        [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
        
        [Header("Notification")]
        [SerializeField] private GameObject objNotificationBadge; 
        [SerializeField] private TMP_Text txtClaimableCount;
        private IPassService _passService;
        public event Action<int, bool> OnClaimRewardClicked;
        public event Action<int> OnClaimBonusClicked;
        public event Action OnBuyPremiumClicked;

        [Inject]
        public void Constructor(IPassService passService)
        {
            _passService = passService;
            _passService.RegisterView(this); 
        }

        private void OnDestroy()
        {
            if (_passService != null)
            {
                _passService.UnregisterView(this);
            }
        }

        public void RefreshUI(PassViewData viewData)
        {
            if (viewData == null) return;
            if (viewData.EventEndTime == DateTime.MinValue || DateTime.UtcNow >= viewData.EventEndTime)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
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

            UpdateExpProgress(viewData);

            int claimableCount = 0;

            if (viewData.Milestones != null)
            {
                foreach (var milestone in viewData.Milestones)
                {
                    if (milestone.FreeState == MilestoneState.ReadyToClaim) claimableCount++;
                    if (milestone.PremiumState == MilestoneState.ReadyToClaim) claimableCount++;
                }
            }

            if (viewData.BonusMilestones != null)
            {
                foreach (var bonus in viewData.BonusMilestones)
                {
                    if (bonus.State == MilestoneState.ReadyToClaim) claimableCount++;
                }
            }

            if (objNotificationBadge != null)
            {
                objNotificationBadge.SetActive(claimableCount > 0);
            }

            if (txtClaimableCount != null)
            {
                txtClaimableCount.text = claimableCount.ToString();
            }
        }
        

        private void UpdateExpProgress(PassViewData viewData)
        {
            if (viewData.Milestones == null || viewData.Milestones.Count == 0) return;

            int totalExpProgress = viewData.CurrentExp;
            int currentLevelIndex = viewData.CurrentMilestoneIndex;
        
            int expInCurrentLevel = 0;
            int expRequiredForNextLevel = 0;
            bool isMaxNormalLevel = true;

            int accumulatedExpBefore = 0;
    
            var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();

            foreach (var milestone in sortedMilestones)
            {
                if (milestone.Index <= currentLevelIndex)
                {
                    accumulatedExpBefore += milestone.RequiredExp;
                }
                if (milestone.Index == currentLevelIndex + 1)
                {
                    expRequiredForNextLevel = milestone.RequiredExp;
                    isMaxNormalLevel = false;
                }
            }

            expInCurrentLevel = Mathf.Max(0, totalExpProgress - accumulatedExpBefore);

            if (isMaxNormalLevel)
            {
                if (sliderExpProgress != null) sliderExpProgress.value = 1f;
                return;
            }
            if (sliderExpProgress != null && expRequiredForNextLevel > 0)
            {
                sliderExpProgress.value = (float)expInCurrentLevel / expRequiredForNextLevel;
            }
        }
    }
}