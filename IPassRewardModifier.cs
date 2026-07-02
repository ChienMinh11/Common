using ChieChie.Constracts;

namespace ChieChie.GamePass
{
    public interface IPassRewardModifier
    {
        bool ShouldReplaceReward(int index, bool isPremium, bool isBonus);
        System.Collections.Generic.List<IItemReward> GetReplacedRewards(int index, bool isPremium, bool isBonus, System.Collections.Generic.List<IItemReward> originalRewards);
    }
}