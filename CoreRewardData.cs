using System;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class CoreRewardData : BaseRewardData
    {
        public CoreRewardData(string type, long amt,Sprite iconReward,Sprite iconInfinityReward ,bool isInfinite, float duration)
        {
            this.resourceType = type;
            this.amount = amt;
            this.iconRewward = iconReward;
            this.iconInfiniteReward = iconInfinityReward;
            this.isInfiniteReward = isInfinite;
            this.infinityDuration = duration;
        }
    }
}
