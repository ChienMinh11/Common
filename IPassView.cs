using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;
using ChieChie.MVP;

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
        public BonusBankUIData BonusBank;
        public int TotalBonusExpEarned; 
        public IReadOnlyList<int> GetNormalMilestoneRequiredPoints() => Milestones?.Select(m => m.RequiredExp).ToList();
        public IReadOnlyList<int> GetBonusMilestoneRequiredPoints()
        {
            if (BonusBank != null && BonusBank.IsAvailable && BonusBank.RequiredExpToMax > 0)
            {
                return new List<int> { BonusBank.RequiredExpToMax };
            }

            return BonusMilestones?.Select(b => b.RequiredExp).ToList();
        }
        public int TotalBonusPointsEarned => TotalBonusExpEarned;
        
    }

    public class BonusBankUIData
    {
        public int CurrentAmount;
        public int MaxAmount;
        public int ExpConvertToAmount;
        public int RequiredExpToMax;
        public bool IsUnlocked;
        public MilestoneState State;
        public UnityEngine.GameObject BonusBankIcon;
        public bool IsAvailable => MaxAmount > 0 && ExpConvertToAmount > 0;

        public int ConvertBonusExpToAmount(int bonusExp)
        {
            if (!IsAvailable || bonusExp <= 0) return 0;

            long amount = (long)bonusExp * ExpConvertToAmount;
            return amount >= MaxAmount ? MaxAmount : (int)amount;
        }
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

    public interface IPassView : IView
    {
        string ViewId { get; }
        event Action<int, bool> OnClaimRewardClicked;
        event Action<int> OnClaimBonusClicked; 
        event Action OnClaimBonusBankClicked;
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
