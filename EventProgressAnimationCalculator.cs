using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Core
{
    public struct MilestoneAnimationStep
    {
        public float FromProgressPercentage { get; set; }
        public float ToProgressPercentage { get; set; }
        public string FromProgressText { get; set; }
        public string ToProgressText { get; set; }
        public int EvaluatedExpForClaimableCheck { get; set; }
    }

    public struct MilestoneProgressState
    {
        public static MilestoneProgressState CreateCompletedState() => new MilestoneProgressState
        {
            ProgressPercentage = 1f,
            ProgressText = "Completed!",
            LevelEndRequiredExp = int.MaxValue,
            CurrentLevelMaxExp = 0,
            IsAllLevelsCompleted = true
        };

        public float ProgressPercentage { get; set; }
        public string ProgressText { get; set; }
        public int LevelEndRequiredExp { get; set; }
        public int CurrentLevelMaxExp { get; set; }
        public bool IsAllLevelsCompleted { get; set; }
    }

   public static class EventProgressAnimationCalculator
    {
        public static IEnumerable<MilestoneAnimationStep> CalculateAnimationSteps(IEventProgressData eventData, int startPoints, int targetPoints)
        {
            var normalRequiredList = eventData.GetNormalMilestoneRequiredPoints();
            
            int totalNormalRequiredPoints = 0;
            if (normalRequiredList != null)
            {
                foreach (var points in normalRequiredList) totalNormalRequiredPoints += points;
            }

            int currentTrackedPoints = Mathf.Max(0, startPoints);
            int finalTargetPoints = Mathf.Max(currentTrackedPoints, targetPoints);

            while (currentTrackedPoints < finalTargetPoints)
            {
                MilestoneProgressState currentProgressState = GetProgressStateAtPoints(eventData, currentTrackedPoints, totalNormalRequiredPoints);
                
                if (currentProgressState.IsAllLevelsCompleted)
                {
                    yield return new MilestoneAnimationStep
                    {
                        FromProgressPercentage = currentProgressState.ProgressPercentage,
                        ToProgressPercentage = 1f,
                        FromProgressText = currentProgressState.ProgressText,
                        ToProgressText = currentProgressState.ProgressText,
                        EvaluatedExpForClaimableCheck = finalTargetPoints
                    };
                    yield break;
                }

                int stepEndPoints = Mathf.Min(finalTargetPoints, currentProgressState.LevelEndRequiredExp);
                bool isLevelBoundaryReached = stepEndPoints >= currentProgressState.LevelEndRequiredExp;

                if (isLevelBoundaryReached)
                {
                    yield return new MilestoneAnimationStep
                    {
                        FromProgressPercentage = currentProgressState.ProgressPercentage,
                        ToProgressPercentage = 1f,
                        FromProgressText = currentProgressState.ProgressText,
                        ToProgressText = $"{currentProgressState.CurrentLevelMaxExp}/{currentProgressState.CurrentLevelMaxExp}",
                        EvaluatedExpForClaimableCheck = stepEndPoints
                    };
                }
                else
                {
                    MilestoneProgressState nextProgressState = GetProgressStateAtPoints(eventData, stepEndPoints, totalNormalRequiredPoints);
                    yield return new MilestoneAnimationStep
                    {
                        FromProgressPercentage = currentProgressState.ProgressPercentage,
                        ToProgressPercentage = nextProgressState.ProgressPercentage,
                        FromProgressText = currentProgressState.ProgressText,
                        ToProgressText = nextProgressState.ProgressText,
                        EvaluatedExpForClaimableCheck = stepEndPoints
                    };
                }

                if (stepEndPoints <= currentTrackedPoints) yield break;
                currentTrackedPoints = stepEndPoints;
            }
        }

        private static MilestoneProgressState GetProgressStateAtPoints(IEventProgressData eventData, int totalAccumulatedPoints, int totalNormalRequiredPoints)
        {
            int currentMilestoneLimitPoints = 0;
            var sortedNormalMilestones = eventData.GetNormalMilestoneRequiredPoints();

            if (sortedNormalMilestones != null)
            {
                foreach (var requiredPoints in sortedNormalMilestones)
                {
                    int levelStartPoints = currentMilestoneLimitPoints;
                    int levelEndPoints = currentMilestoneLimitPoints + requiredPoints;
                    
                    if (totalAccumulatedPoints < levelEndPoints)
                    {
                        int pointsInCurrentLevel = Mathf.Max(0, totalAccumulatedPoints - levelStartPoints);
                        float progressFraction = requiredPoints > 0 ? (float)pointsInCurrentLevel / requiredPoints : 0f;
                        
                        return new MilestoneProgressState
                        {
                            ProgressPercentage = progressFraction,
                            ProgressText = $"{pointsInCurrentLevel}/{requiredPoints}",
                            LevelEndRequiredExp = levelEndPoints,
                            CurrentLevelMaxExp = requiredPoints
                        };
                    }

                    currentMilestoneLimitPoints = levelEndPoints;
                }
            }

            var sortedBonusMilestones = eventData.GetBonusMilestoneRequiredPoints();
            if (sortedBonusMilestones == null || sortedBonusMilestones.Count == 0)
            {
                return MilestoneProgressState.CreateCompletedState();
            }

            int currentBonusPoints = Mathf.Max(0, totalAccumulatedPoints - totalNormalRequiredPoints);
            int accumulatedBonusPointsBefore = 0;
            
            foreach (var requiredPoints in sortedBonusMilestones)
            {
                if (currentBonusPoints < requiredPoints)
                {
                    int currentLevelMaxBonusPoints = requiredPoints - accumulatedBonusPointsBefore;
                    int pointsInCurrentBonusLevel = Mathf.Max(0, currentBonusPoints - accumulatedBonusPointsBefore);
                    float progressFraction = currentLevelMaxBonusPoints > 0 ? (float)pointsInCurrentBonusLevel / currentLevelMaxBonusPoints : 0f;
                    
                    return new MilestoneProgressState
                    {
                        ProgressPercentage = progressFraction,
                        ProgressText = $"{pointsInCurrentBonusLevel}/{currentLevelMaxBonusPoints}",
                        LevelEndRequiredExp = totalNormalRequiredPoints + requiredPoints,
                        CurrentLevelMaxExp = currentLevelMaxBonusPoints
                    };
                }

                accumulatedBonusPointsBefore = requiredPoints;
            }

            return MilestoneProgressState.CreateCompletedState();
        }
    }
}
