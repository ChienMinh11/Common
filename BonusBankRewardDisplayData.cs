using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace Game.GamePlay
{
    public class BonusBankRewardDisplayData : IBaseRewardDisplayData
    {
        private readonly List<IItemReward> _rewards = new List<IItemReward>();
        public  BonusBankRewardDisplayData(List<IItemReward> rewardDatas)
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
