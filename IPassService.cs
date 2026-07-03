using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassService 
    { 
        event Action<List<IItemReward>> OnRewardsClaimed;
        event Action<List<IItemReward>> OnAutoClaimedRewardsProcessed;
        event Action<IPassNotificationEventData> OnAutoClaimNotificationTriggered;
        List<IItemReward> GetAndClearAutoClaimedRewards();
        void RegisterView(IPassView view);
        void UnregisterView(IPassView view);
        void AddExp(int amount);
        void AddExp(int amount, bool delayUpdateUI);
        void FlushDelayedUIUpdate();
        void FlushDelayedUIUpdate(IPassView view);
        void RegisterRewardModifier(IPassRewardModifier modifier);
        void UnregisterRewardModifier(IPassRewardModifier modifier);

        void CheckEventUpdate();
        DateTime EventEndTime {get; }
        void ActiveNewEvent();
        void UnlockPremiumPass();
        void RefreshData();

    }
}