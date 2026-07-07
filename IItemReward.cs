using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IItemReward
    {
        string ResourceId{get;}
        long Amount { get; }
        bool IsInfiniteReward{get;}
        float InfinityDuration{get;}
        Sprite IconReward{get;}
        Sprite InfinityRewardIcon{get;}
    }
}
