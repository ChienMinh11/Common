using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassNotificationEventData : IPassNotificationEventData
    {
         public List<IItemReward> Rewards { get; }
        public bool IsBonusData { get; }

        public PassNotificationEventData(List<IItemReward> rewards, bool isBonusData)
        {
            Rewards = rewards;
            IsBonusData = isBonusData;
        }
    }
}
