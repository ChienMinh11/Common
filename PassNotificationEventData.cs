using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassNotificationEventData : IPassNotificationEventData
    {
         public List<IItemReward> Rewards { get; }
        public bool IsBonusData { get; }

        public bool IsBonusBank { get; } 

        public PassNotificationEventData(List<IItemReward> rewards, bool isBonusData, bool isBonusBank = false)
        {
            Rewards = rewards;
            IsBonusData = isBonusData;
            IsBonusBank = isBonusBank;
        }
    }
}
