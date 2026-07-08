using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;

namespace ChieChie.GamePass
{
    public class PassViewData:IEventProgressData
    {
        public DateTime EventEndTime;
        public int CurrentExp;
        public int CurrentMilestoneIndex;
        public bool IsPremiumUnlocked;
        public List<MilestoneUIData> Milestones;
        public List<BonusMilestoneUIData> BonusMilestones; 
        public int TotalBonusExpEarned; 
        public IReadOnlyList<int> GetNormalMilestoneRequiredPoints() => Milestones?.Select(m => m.RequiredExp).ToList();
        public IReadOnlyList<int> GetBonusMilestoneRequiredPoints() => BonusMilestones?.Select(b => b.RequiredExp).ToList();
        public int TotalBonusPointsEarned => TotalBonusExpEarned;
        
    }
    public class BonusMilestoneUIData
    {
        public int Index;
        public int RequiredExp;
        public List<IItemReward> Rewards;
        public MilestoneState State;
        public UnityEngine.GameObject BonusIcon;
    }

    public class MilestoneUIData
    {
        public int Index;
        public int RequiredExp;
        public List<IItemReward> FreeRewards;
        public List<IItemReward> PremiumRewards;
        public MilestoneState FreeState;
        public MilestoneState PremiumState;
        public UnityEngine.GameObject CustomIconFreePass;
        public UnityEngine.GameObject CustomIconPremiumPass;
    }

    public interface IPassView 
    {
        string ViewId { get; }
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