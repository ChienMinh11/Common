using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using UnityEngine;

namespace Game.GamePlay
{
    public static class RewardClaimedEventDataHelper
    {
        public static List<RewardClaimedEventData> FromRewardDisplayData(IBaseRewardDisplayData data)
        {
            return data == null
                ? new List<RewardClaimedEventData>()
                : FromBaseRewards(data.GetRewards());
        }

        public static List<RewardClaimedEventData> FromItemRewards(IEnumerable<IItemReward> rewards)
        {
            var rewardDataList = new List<RewardClaimedEventData>();
            if (rewards == null) return rewardDataList;

            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                rewardDataList.Add(FromItemReward(reward));
            }

            return rewardDataList;
        }

        public static RewardClaimedEventData FromItemReward(IItemReward reward)
        {
            string typeKey = NormalizeResourceType(reward.ResourceId);
            Sprite rewardSprite = reward.IsInfiniteReward && reward.InfinityRewardIcon != null
                ? reward.InfinityRewardIcon
                : reward.IconReward;

            return Create(
                typeKey,
                reward.Amount,
                rewardSprite,
                reward.IsInfiniteReward,
                reward.InfinityDuration
            );
        }

        public static List<RewardClaimedEventData> FromBaseRewards(IEnumerable<BaseRewardData> rewards)
        {
            var rewardDataList = new List<RewardClaimedEventData>();
            if (rewards == null) return rewardDataList;

            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                rewardDataList.Add(FromBaseReward(reward));
            }

            return rewardDataList;
        }

        public static RewardClaimedEventData FromBaseReward(BaseRewardData reward)
        {
            string typeKey = NormalizeResourceType(reward.resourceType);
            Sprite rewardSprite = reward.isInfiniteReward && reward.iconInfiniteReward != null
                ? reward.iconInfiniteReward
                : reward.iconRewward;

            return Create(
                typeKey,
                reward.amount,
                rewardSprite,
                reward.isInfiniteReward,
                reward.infinityDuration
            );
        }

        private static RewardClaimedEventData Create(
            string typeKey,
            long amount,
            Sprite rewardSprite,
            bool isInfiniteReward,
            float infinityDuration)
        {
            return new RewardClaimedEventData
            {
                ResourceType = typeKey,
                Amount = ClampRewardAmount(amount),
                RewardSprite = rewardSprite,
                AmountDisplayText = isInfiniteReward
                    ? CoreExtensions.FormatTime(infinityDuration)
                    : FormatAmount(typeKey, amount),
                ShowAmountText = ShouldShowAmountText(typeKey)
            };
        }

        private static string NormalizeResourceType(string resourceType)
        {
            return string.IsNullOrEmpty(resourceType) ? string.Empty : resourceType.Trim();
        }

        private static string FormatAmount(string typeKey, long amount)
        {
            return ShouldShowPrefix(typeKey) ? $"x{amount}" : $"{amount}";
        }

        private static bool ShouldShowPrefix(string typeKey)
        {
            return typeKey != "Gold";
        }

        private static bool ShouldShowAmountText(string typeKey)
        {
            return typeKey != "Gold" && typeKey != "Lives";
        }

        private static int ClampRewardAmount(long amount)
        {
            if (amount > int.MaxValue) return int.MaxValue;
            if (amount < int.MinValue) return int.MinValue;
            return (int)amount;
        }
    }
}
