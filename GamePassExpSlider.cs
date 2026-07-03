using System.Linq;
using ChieChie.GamePass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class GamePassExpSlider : MonoBehaviour
    {
        private const string COMPLETED = "Completed!";

        [SerializeField] private Slider sliderExpProgress;
        [SerializeField] private TMP_Text txtCurrentExp;


        public bool UpdateProgressDetailed(PassViewData viewData)
        {
            if (viewData.Milestones == null || viewData.Milestones.Count == 0) return false;

            CalculateNormalProgress(viewData, out int expInCurrentLevel, out int expRequiredForNextLevel, out bool isMaxNormalLevel);

            if (!isMaxNormalLevel)
            {
                if (txtCurrentExp != null) txtCurrentExp.text = $"{expInCurrentLevel}/{expRequiredForNextLevel}";
                if (sliderExpProgress != null && expRequiredForNextLevel > 0)
                {
                    sliderExpProgress.value = (float)expInCurrentLevel / expRequiredForNextLevel;
                }
                return false; 
            }

            if (viewData.BonusMilestones == null || viewData.BonusMilestones.Count == 0)
            {
                if (txtCurrentExp != null) txtCurrentExp.text = COMPLETED;
                if (sliderExpProgress != null) sliderExpProgress.value = 1f;
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
                if (txtCurrentExp != null) txtCurrentExp.text = COMPLETED;
                if (sliderExpProgress != null) sliderExpProgress.value = 1f;
                return true;
            }

            int expInCurrentBonus = Mathf.Max(0, bonusExp - accumulatedBonusExpBefore);

            if (txtCurrentExp != null) txtCurrentExp.text = $"{expInCurrentBonus}/{bonusExpRequiredForNext}";
            if (sliderExpProgress != null && bonusExpRequiredForNext > 0)
            {
                sliderExpProgress.value = (float)expInCurrentBonus / bonusExpRequiredForNext;
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
        }
    }
}