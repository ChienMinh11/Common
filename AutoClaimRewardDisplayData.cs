using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using UnityEngine;

namespace Game.GamePlay
{
    public class AutoClaimRewardDisplayData : IBaseRewardDisplayData
    {
        private readonly List<BaseRewardData> _rewards = new List<BaseRewardData>();
        public AutoClaimRewardDisplayData(List<IItemReward> shopRewards)
        {
            if (shopRewards != null)
            {
                foreach (var r in shopRewards) 
                {
                    var coreReward = new CoreRewardData(
                        r.ResourceId, 
                        r.Amount, 
                        r.IconReward,
                        r.InfinityRewardIcon,
                        r.IsInfiniteReward, 
                        r.InfinityDuration
                    );
                    _rewards.Add(coreReward); 
                }
            }
        }
        public List<BaseRewardData> GetRewards() => _rewards;
        public string GetTitle() => "Your Rewards!";
        public string GetDescription() => "";
        public string GetName() => "ShopReward";
        public void OnRewardsClaimed() { /* Logic khi bấm nhận */ }
        public void OnClosePopup() { }
    }
}
