using System.Collections.Generic;
using ChieChie.Constracts;
namespace Game.GamePlay
{
    public class AutoClaimRewardDisplayData : IBaseRewardDisplayData
    {
        private readonly List<IItemReward> _rewards = new List<IItemReward>();
        public AutoClaimRewardDisplayData(List<IItemReward> rewardDatas)
        {
            _rewards = rewardDatas ?? new List<IItemReward>();
        }
        public List<IItemReward> GetRewards() => _rewards;
        public string GetTitle() => "Your Rewards!";
        public string GetDescription() => "";
        public string GetName() => "End Pass Reward";
        public void OnRewardsClaimed() { /* Logic khi bấm nhận */ }
        public void OnClosePopup() { }
    }
}
