using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Core
{
    public static class RewardClaimedEventDataHelper
    {
        public static List<RewardClaimedEventData> FromRewardDisplayData(
            IBaseRewardDisplayData data, 
            bool forceShowGoldPrefix = false, 
            bool forceShowGoldAmountText = false)
        {
            return data == null
                ? new List<RewardClaimedEventData>()
                : FromBaseRewards(data.GetRewards(), forceShowGoldPrefix, forceShowGoldAmountText);
        }

        public static List<RewardClaimedEventData> FromItemRewards(
            IEnumerable<IItemReward> rewards, 
            bool forceShowGoldPrefix = false, 
            bool forceShowGoldAmountText = false)
        {
            var rewardDataList = new List<RewardClaimedEventData>();
            if (rewards == null) return rewardDataList;

            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                rewardDataList.Add(FromItemReward(reward, forceShowGoldPrefix, forceShowGoldAmountText));
            }

            return rewardDataList;
        }

        public static RewardClaimedEventData FromItemReward(
            IItemReward reward, 
            bool forceShowGoldPrefix = false, 
            bool forceShowGoldAmountText = false)
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
                reward.InfinityDuration,
                forceShowGoldPrefix,
                forceShowGoldAmountText
            );
        }

        public static List<RewardClaimedEventData> FromBaseRewards(
            IEnumerable<IItemReward> rewards, 
            bool forceShowGoldPrefix = false, 
            bool forceShowGoldAmountText = false)
        {
            var rewardDataList = new List<RewardClaimedEventData>();
            if (rewards == null) return rewardDataList;

            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                rewardDataList.Add(FromBaseReward(reward, forceShowGoldPrefix, forceShowGoldAmountText));
            }

            return rewardDataList;
        }

        public static RewardClaimedEventData FromBaseReward(
            IItemReward reward, 
            bool forceShowGoldPrefix = false, 
            bool forceShowGoldAmountText = false)
        {
            string typeKey = NormalizeResourceType(reward.ResourceId);
            Sprite rewardSprite = reward.IsInfiniteReward && reward.IsInfiniteReward != null
                ? reward.InfinityRewardIcon
                : reward.IconReward;

            return Create(
                typeKey,
                reward.Amount,
                rewardSprite,
                reward.IsInfiniteReward,
                reward.InfinityDuration,
                forceShowGoldPrefix,
                forceShowGoldAmountText
            );
        }

        private static RewardClaimedEventData Create(
            string typeKey,
            long amount,
            Sprite rewardSprite,
            bool isInfiniteReward,
            float infinityDuration,
            bool forceShowPrefix,
            bool forceShowAmountText
            )
        {
            return new RewardClaimedEventData
            {
                ResourceType = typeKey,
                Amount = ClampRewardAmount(amount),
                RewardSprite = rewardSprite,
                AmountDisplayText = isInfiniteReward
                    ? CoreExtensions.FormatTime(infinityDuration)
                    : FormatAmount(typeKey, amount, forceShowPrefix),
                ShowAmountText = ShouldShowAmountText(typeKey, forceShowAmountText)
            };
        }

        private static string NormalizeResourceType(string resourceType)
        {
            return string.IsNullOrEmpty(resourceType) ? string.Empty : resourceType.Trim();
        }

        private static string FormatAmount(string typeKey, long amount, bool forceShowGoldPrefix)
        {
            return ShouldShowPrefix(typeKey, forceShowGoldPrefix) ? $"x{amount}" : $"{amount}";
        }

        private static bool ShouldShowPrefix(string typeKey, bool forceShowPrefix)
        {
   
            if (typeKey == "Gold" && forceShowPrefix) return true;
            
            return typeKey != "Gold";
        }

        private static bool ShouldShowAmountText(string typeKey, bool forceShowAmountText)
        {
            if (typeKey == "Gold" && forceShowAmountText) return true;

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