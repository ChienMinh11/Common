using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopRewardDisplayData : IBaseRewardDisplayData
    {
        private readonly List<BaseRewardData> _rewards = new List<BaseRewardData>();
    
        public ShopRewardDisplayData(List<ShopItemReward> shopRewards)
        {
            if (shopRewards != null)
            {
                foreach (var r in shopRewards) _rewards.Add(r); 
            }
        }
        public List<BaseRewardData> GetRewards() => _rewards;
        public string GetTitle() => "Your Rewards!";
        public string GetDescription() => "";
        public string GetName() => "ShopReward";
        public void OnRewardsClaimed() { /* Logic riêng của shop khi bấm nhận */ }
        public void OnClosePopup() { }
    }
}
