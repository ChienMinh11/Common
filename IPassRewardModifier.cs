namespace ChieChie.GamePass
{
    public interface IPassRewardModifier
    {
        bool ShouldReplaceReward(int index, bool isPremium, bool isBonus);
        System.Collections.Generic.List<PassRewardData> GetReplacedRewards(int index, bool isPremium, bool isBonus, System.Collections.Generic.List<PassRewardData> originalRewards);
    }
}