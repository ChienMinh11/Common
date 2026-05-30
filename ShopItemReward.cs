using System;

namespace ChieChie.Core
{
    [Serializable]
    public class ShopItemReward : BaseRewardData
    {
        public RewardType rewardType = RewardType.Resource;
        
        // Giữ lại cho backward compatibility
        public bool isInfinite
        {
            get => isInfiniteReward;
            set => isInfiniteReward = value;
        }
        
        public float infiniteDuration
        {
            get => infinityDuration;
            set => infinityDuration = value;
        }
    }
}
