using System;
using System.Collections.Generic;
using ChieChie.Constracts;

namespace ChieChie.GamePass
{
    public enum PassRewardSource
    {
        Normal,     
        Premium,    
        Bonus,       
        BonusBank    
    }
    public interface IPassService 
    { 
        event Action OnDataChanged;
        event Action<List<IItemReward>, PassRewardSource> OnRewardsClaimed;
        event Action<List<IItemReward>> OnAutoClaimedRewardsProcessed;
        event Action<IPassNotificationEventData> OnAutoClaimNotificationTriggered;
        event Action<IPassNotificationEventData> OnBonusBankClaimNotificationTriggered;
        List<IItemReward> GetAndClearAutoClaimedRewards();
        void BindView(IPassView view);
        void UnbindView(IPassView view);
        PassViewData GetViewData(string viewId);
        PassViewData GetCurrentViewData();
        void AddExp(int amount, bool delayUpdateUI);
        PassViewData FlushDelayedUIUpdate(string viewId);
        void RegisterRewardModifier(IPassRewardModifier modifier);
        void UnregisterRewardModifier(IPassRewardModifier modifier);
        void CheckEventUpdate();
        DateTime EventEndTime {get; }
        bool IsEventActive { get; }
        void ActiveNewEvent();
        void UnlockPremiumPass();
        void RefreshData();
        bool IsFirstOpen { get; }
        void MarkFirstOpenCompleted();

    }
}
