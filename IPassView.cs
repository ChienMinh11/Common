using System;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    public class PassViewData
    {
        public string RemainingTimeStr;
        public int CurrentExp;
        public int CurrentMilestoneIndex;
        public bool IsPremiumUnlocked;
        public List<MilestoneUIData> Milestones;

        public List<BonusMilestoneUIData> BonusMilestones; 
        public int TotalBonusExpEarned; 
    }
    public class BonusMilestoneUIData
    {
        public int Index;
        public int RequiredExp;
        public List<PassRewardData> Rewards;
        public MilestoneState State;
    }

    public class MilestoneUIData
    {
        public int Index;
        public int RequiredExp;
        public List<PassRewardData> FreeRewards;
        public List<PassRewardData> PremiumRewards;
        public MilestoneState FreeState;
        public MilestoneState PremiumState;
    }

    public interface IPassView 
    {
        event Action<int, bool> OnClaimRewardClicked;
        event Action<int> OnClaimBonusClicked; 
        event Action OnBuyPremiumClicked;

        void RefreshUI(PassViewData viewData);
    }
    public enum MilestoneState
    {
        Locked,
        ReadyToClaim,
        Claimed
    }
}