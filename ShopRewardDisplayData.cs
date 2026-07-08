using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Shop;

namespace Game.GamePlay
{
    public class ShopRewardDisplayData : IBaseRewardDisplayData
    {
        private readonly List<IItemReward> _rewards;
    
        public ShopRewardDisplayData(List<IItemReward> shopRewards)
        {
            _rewards = shopRewards ?? new List<IItemReward>(); // Gán trực tiếp, không cần loop map data nữa!
        }
        
        public List<IItemReward> GetRewards() => _rewards;
        public string GetTitle() => "Your Rewards!";
        public string GetDescription() => "";
        public string GetName() => "ShopReward";
        public void OnRewardsClaimed() { /* Logic khi bấm nhận */ }
        public void OnClosePopup() { }
    }
}
