using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using Game.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
 
        [Tooltip("Kích hoạt khi thanh slider chạy đầy 100% (Lên cấp)")]
        public UnityEvent JuiceEffect; 

        public bool UpdateProgressDetailed(PassViewData viewData)
        {
            if (viewData?.Milestones == null || viewData.Milestones.Count == 0) return false;

            bool isBonusState = GetProgressData(viewData, out string expText, out float progressFraction);
            if (txtCurrentExp != null) txtCurrentExp.text = expText;
            
            StopAnimation();
            UpdateVisualProgress(progressFraction);

            return isBonusState;
        }

        public void SetProgress(float progressFraction, string expText)
        {
            if (txtCurrentExp != null) txtCurrentExp.text = expText;
            UpdateVisualProgress(progressFraction);
        }

        public async UniTask PlaySliderAnimationAsync(float fromProgress, float toProgress, string fromText, string toText, CancellationToken ct, float durationOverride = -1f)
        {
            if (sliderExpProgress != null) sliderExpProgress.value = fromProgress;
            if (filledImage != null) filledImage.fillAmount = fromProgress;

            bool isFromValid = TryParseExpText(fromText, out int fromValue, out int fromMax);
            bool isToValid = TryParseExpText(toText, out int toValue, out int toMax);

            float elapsed = 0f;
            float finalDuration = durationOverride > 0f ? durationOverride : animationDuration;

            PlayTextPunchAnimation(ct).Forget();

            bool hasTriggeredJuice = false;

            while (elapsed < finalDuration)
            {
                elapsed += Time.deltaTime;
                float t = finalDuration > 0f ? elapsed / finalDuration : 1f;
                float currentProgress = Mathf.Lerp(fromProgress, toProgress, t);
                UpdateVisualProgress(currentProgress);

                if (currentProgress >= 1f && !hasTriggeredJuice)
                {
                    hasTriggeredJuice = true;
                    JuiceEffect?.Invoke();  
                     
                }

                if (txtCurrentExp != null)
                {
                    if (isFromValid && isToValid && fromMax == toMax)
                    {
                        int currentExpValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, t));
                        txtCurrentExp.text = $"{currentExpValue}/{fromMax}";
                    }
                    else
                    {
                        txtCurrentExp.text = fromText;
                    }
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            UpdateVisualProgress(toProgress);
            if (txtCurrentExp != null) txtCurrentExp.text = toText;
        
            if (toProgress >= 1f && !hasTriggeredJuice)
            {
                JuiceEffect?.Invoke();
            }
        }

        private bool TryParseExpText(string text, out int currentExp, out int maxExp)
        {
            currentExp = 0;
            maxExp = 0;
            if (string.IsNullOrEmpty(text)) return false;

            var match = Regex.Match(text, @"^(\d+)/(\d+)$");
            if (match.Success)
            {
                currentExp = int.Parse(match.Groups[1].Value);
                maxExp = int.Parse(match.Groups[2].Value);
                return true;
            }
            return false;
        }

        private async UniTaskVoid PlayTextPunchAnimation(CancellationToken ct)
        {
            if (txtCurrentExp == null) return;

            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = originalScale * 1.2f;
            
            txtCurrentExp.transform.localScale = punchScale;
            
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (txtCurrentExp == null) return;
                txtCurrentExp.transform.localScale = Vector3.Lerp(punchScale, originalScale, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (txtCurrentExp != null) txtCurrentExp.transform.localScale = originalScale;
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

            if (TryGetBonusBankProgress(viewData, out expText, out progressFraction))
            {
                return true;
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

        private bool TryGetBonusBankProgress(PassViewData viewData, out string expText, out float progressFraction)
        {
            expText = string.Empty;
            progressFraction = 0f;

            var bonusBank = viewData.BonusBank;
            if (bonusBank == null || !bonusBank.IsAvailable)
            {
                return false;
            }

            int currentAmount = Mathf.Clamp(bonusBank.CurrentAmount, 0, bonusBank.MaxAmount);
            expText = $"{currentAmount}/{bonusBank.MaxAmount}";
            progressFraction = bonusBank.MaxAmount > 0 ? (float)currentAmount / bonusBank.MaxAmount : 0f;
            return true;
        }

        public static bool TryGetBonusBankProgressTextAtExp(PassViewData viewData, int totalExp, out string progressText)
        {
            progressText = string.Empty;

            var bonusBank = viewData?.BonusBank;
            if (bonusBank == null || !bonusBank.IsAvailable) return false;

            int totalNormalRequiredExp = GetTotalNormalRequiredExp(viewData);
            if (totalExp < totalNormalRequiredExp) return false;

            int bonusExp = Mathf.Max(0, totalExp - totalNormalRequiredExp);
            int currentAmount = bonusBank.ConvertBonusExpToAmount(bonusExp);
            progressText = $"{currentAmount}/{bonusBank.MaxAmount}";
            return true;
        }

        private static int GetTotalNormalRequiredExp(PassViewData viewData)
        {
            if (viewData?.Milestones == null) return 0;

            int totalRequiredExp = 0;
            foreach (var milestone in viewData.Milestones)
            {
                totalRequiredExp += milestone.RequiredExp;
            }

            return totalRequiredExp;
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
