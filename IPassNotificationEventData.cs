using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassNotificationEventData 
    {
        System.Collections.Generic.List<IItemReward> Rewards { get; }
        bool IsBonusData { get; }
        
    }
}
