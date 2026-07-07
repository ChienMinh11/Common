using UnityEngine;

namespace Game.GamePlay
{
    public struct RewardClaimedEventData
    {
        public string ResourceType { get; set; } 
        public int Amount { get; set; }
        public Sprite RewardSprite { get; set; } 
        public string AmountDisplayText { get; set; } 
        public bool ShowAmountText { get; set; } 
    }
}
