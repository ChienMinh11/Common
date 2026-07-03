using System;
using System.Linq;
using System.Threading;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using Game.Extensions; // Import namespace chứa Extension của bạn
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class GamePassExpSlider : MonoBehaviour
    {
        private const string COMPLETED = "Completed!";

        [SerializeField] private Slider sliderExpProgress;
        [SerializeField] private Image filledImage;
        [SerializeField] private TMP_Text txtCurrentExp;
        [SerializeField] private float animationDuration = 0.5f;
        
        private CancellationTokenSource _animationCts;

        public bool UpdateProgressDetailed(PassViewData viewData)
        {
            if (viewData?.Milestones == null || viewData.Milestones.Count == 0) return false;

            bool isBonusState = GetProgressData(viewData, out string expText, out float progressFraction);
            if (txtCurrentExp != null) txtCurrentExp.text = expText;
            
            StopAnimation();
            UpdateVisualProgress(progressFraction);

            return isBonusState;
        }

        public async UniTask<bool> UpdateProgressDetailedAsync(PassViewData viewData)
        {
            if (viewData?.Milestones == null || viewData.Milestones.Count == 0) return false;

            bool isBonusState = GetProgressData(viewData, out string expText, out float targetProgress);
            if (txtCurrentExp != null) txtCurrentExp.text = expText;

            if (gameObject.activeInHierarchy)
            {
                StopAnimation();
                _animationCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
              
                await UniTask.WhenAll(
                    sliderExpProgress.LerpValueAsync(targetProgress, animationDuration, _animationCts.Token),
                    filledImage.LerpFillAmountAsync(targetProgress, animationDuration, _animationCts.Token)
                );
            }
            else
            {
                StopAnimation();
                UpdateVisualProgress(targetProgress);
            }

            return isBonusState;
        }

        private bool GetProgressData(PassViewData viewData, out string expText, out float progressFraction)
        {
            CalculateNormalProgress(viewData, out int expInCurrentLevel, out int expRequiredForNextLevel, out bool isMaxNormalLevel);

            if (!isMaxNormalLevel)
            {
                expText = $"{expInCurrentLevel}/{expRequiredForNextLevel}";
                progressFraction = expRequiredForNextLevel > 0 ? (float)expInCurrentLevel / expRequiredForNextLevel : 0f;
                return false; 
            }

            if (viewData.BonusMilestones == null || viewData.BonusMilestones.Count == 0)
            {
                expText = COMPLETED;
                progressFraction = 1f;
                return true;
            }

            int bonusExp = viewData.TotalBonusExpEarned;
            var sortedBonus = viewData.BonusMilestones.OrderBy(b => b.Index).ToList();
            int currentBonusMilestoneIndex = -1;
            int accumulatedBonusExpBefore = 0;
            int bonusExpRequiredForNext = 0;
            bool isMaxAllBonus = true;

            foreach (var bonus in sortedBonus)
            {
                if (bonusExp >= bonus.RequiredExp)
                {
                    currentBonusMilestoneIndex = bonus.Index;
                    accumulatedBonusExpBefore = bonus.RequiredExp;
                }
                if (bonus.Index > currentBonusMilestoneIndex)
                {
                    bonusExpRequiredForNext = bonus.RequiredExp - accumulatedBonusExpBefore;
                    isMaxAllBonus = false;
                    break;
                }
            }

            if (isMaxAllBonus)
            {
                expText = COMPLETED;
                progressFraction = 1f;
            }
            else
            {
                int expInCurrentBonus = Mathf.Max(0, bonusExp - accumulatedBonusExpBefore);
                expText = $"{expInCurrentBonus}/{bonusExpRequiredForNext}";
                progressFraction = bonusExpRequiredForNext > 0 ? (float)expInCurrentBonus / bonusExpRequiredForNext : 0f;
            }

            return true;
        }

        private void CalculateNormalProgress(PassViewData viewData, out int expInCurrentLevel, out int expRequiredForNextLevel, out bool isMaxNormalLevel)
        {
            int totalExpProgress = viewData.CurrentExp;
            int currentLevelIndex = viewData.CurrentMilestoneIndex;

            expInCurrentLevel = 0;
            expRequiredForNextLevel = 0;
            isMaxNormalLevel = true;
            int accumulatedExpBefore = 0;
            
            var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();
            foreach (var milestone in sortedMilestones)
            {
                if (milestone.Index <= currentLevelIndex) accumulatedExpBefore += milestone.RequiredExp;
                if (milestone.Index == currentLevelIndex + 1)
                {
                    expRequiredForNextLevel = milestone.RequiredExp;
                    isMaxNormalLevel = false;
                }
            }
            expInCurrentLevel = Mathf.Max(0, totalExpProgress - accumulatedExpBefore);
        }

        private void UpdateVisualProgress(float value)
        {
            if (sliderExpProgress != null) sliderExpProgress.value = value;
            if (filledImage != null) filledImage.fillAmount = value;
        }

        private void StopAnimation()
        {
            if (_animationCts != null)
            {
                _animationCts.Cancel();
                _animationCts.Dispose();
                _animationCts = null;
            }
        }

        private void OnDisable() => StopAnimation();
    }
}