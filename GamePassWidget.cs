using System;
using System.Linq;
using ChieChie.GamePass;
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
            if (_passService != null)
            {
                _passService.UnregisterView(this);
            }
        }

        [Button]
        private void RefreshUIManual()
        {
            _passService.FlushDelayedUIUpdate(this);
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
            if (expSlider != null)
            {
                expSlider.UpdateProgressDetailed(viewData);
            }
        }
    }
}
