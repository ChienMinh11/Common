using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassService 
    { 
        event Action<List<PassRewardData>> OnRewardsClaimed;
        List<PassRewardData> GetAndClearAutoClaimedRewards();
       void RegisterView(IPassView view);
       void UnregisterView(IPassView view);
       void AddExp(int amount);
       void RegisterRewardModifier(IPassRewardModifier modifier);
       void UnregisterRewardModifier(IPassRewardModifier modifier);

       void CheckEventUpdate();
       DateTime EventEndTime {get; }

    }
}
