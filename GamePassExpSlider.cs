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
            SetProgress(progressFraction, expText);
            StopAnimation();

            return isBonusState;
        }

        public void SetProgress(float progressFraction, string expText)
        {
            if (txtCurrentExp != null) txtCurrentExp.text = expText;
            UpdateVisualProgress(progressFraction);
        }

        public async UniTask PlaySliderAnimationAsync(float fromProgress, float toProgress, string expText, CancellationToken ct)
        {
            if (txtCurrentExp != null) txtCurrentExp.text = expText;

            if (sliderExpProgress != null) sliderExpProgress.value = fromProgress;
            if (filledImage != null) filledImage.fillAmount = fromProgress;

            await UniTask.WhenAll(
                sliderExpProgress.LerpValueAsync(toProgress, animationDuration, ct),
                filledImage.LerpFillAmountAsync(toProgress, animationDuration, ct)
            );
        }

        public bool TryGetProgressDataForExp(PassViewData viewData, int totalExp, out string expText, out float progressFraction)
        {
            expText = string.Empty;
            progressFraction = 0f;

            if (viewData?.Milestones == null || viewData.Milestones.Count == 0) return false;

            CalculateNormalProgress(viewData, totalExp, out int expInCurrentLevel, out int expRequiredForNextLevel, out bool isMaxNormalLevel);

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

            int totalNormalRequiredExp = viewData.Milestones.Sum(m => m.RequiredExp);
            int bonusExp = Mathf.Max(0, totalExp - totalNormalRequiredExp);
            var sortedBonus = viewData.BonusMilestones.OrderBy(b => b.Index).ToList();
            int accumulatedBonusExpBefore = 0;

            foreach (var bonus in sortedBonus)
            {
                if (bonusExp < bonus.RequiredExp)
                {
                    int bonusExpRequiredForNext = bonus.RequiredExp - accumulatedBonusExpBefore;
                    int expInCurrentBonus = Mathf.Max(0, bonusExp - accumulatedBonusExpBefore);
                    expText = $"{expInCurrentBonus}/{bonusExpRequiredForNext}";
                    progressFraction = bonusExpRequiredForNext > 0 ? (float)expInCurrentBonus / bonusExpRequiredForNext : 0f;
                    return true;
                }

                accumulatedBonusExpBefore = bonus.RequiredExp;
            }

            expText = COMPLETED;
            progressFraction = 1f;
            return true;
        }

        private bool GetProgressData(PassViewData viewData, out string expText, out float progressFraction)
        {
            return TryGetProgressDataForExp(viewData, viewData.CurrentExp, out expText, out progressFraction);
        }

        private void CalculateNormalProgress(PassViewData viewData, int totalExpProgress, out int expInCurrentLevel, out int expRequiredForNextLevel, out bool isMaxNormalLevel)
        {
            expInCurrentLevel = 0;
            expRequiredForNextLevel = 0;
            isMaxNormalLevel = true;
            int accumulatedExpBefore = 0;
            
            var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();
            foreach (var milestone in sortedMilestones)
            {
                int accumulatedExpAfter = accumulatedExpBefore + milestone.RequiredExp;
                if (totalExpProgress < accumulatedExpAfter)
                {
                    expInCurrentLevel = Mathf.Max(0, totalExpProgress - accumulatedExpBefore);
                    expRequiredForNextLevel = milestone.RequiredExp;
                    isMaxNormalLevel = false;
                    return;
                }

                accumulatedExpBefore = accumulatedExpAfter;
            }
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