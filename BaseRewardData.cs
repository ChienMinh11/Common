// File: BaseRewardData.cs

using System;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public abstract class BaseRewardData
    {
        public string resourceType;
        public long amount;
        public Sprite iconRewward;
        public Sprite iconInfiniteReward;
        
        [Header("Infinite Reward")]
        public bool isInfiniteReward;
        public float infinityDuration;
        
        public virtual long GetAmount()
        {
            return isInfiniteReward ? 0 : amount;
        }
        
        public bool IsInfiniteReward()
        {
            return isInfiniteReward;
        }
        
        public float GetInfinityDuration()
        {
            return infinityDuration;
        }
        public long GetRandomAmount()
        {
            return GetAmount();
        }
    }
}