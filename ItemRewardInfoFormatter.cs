using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Core
{
    public static class ItemRewardInfoFormatter
    {
        public static Sprite GetRewardIcon(IItemReward reward)
        {
            if (reward == null) return null;

            if (reward.IsInfiniteReward && reward.InfinityRewardIcon != null)
            {
                return reward.InfinityRewardIcon;
            }

            return reward.IconReward;
        }

        public static string GetAmountText(IItemReward reward, bool showPrefix = true)
        {
            if (reward == null) return string.Empty;

            if (reward.IsInfiniteReward)
            {
                return CoreExtensions.FormatTime(reward.InfinityDuration);
            }

            return showPrefix ? $"x{reward.Amount}" : $"{reward.Amount}";
        }
    }
}
