using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassBonusItem", menuName = "CORE/GamePass/Pass Bonus Data")]
    public class PassBonusData: ScriptableObject
    {
        public int index;
        public int expRequied;
        public GameObject bonusIcon;
        public List<PassRewardData> bonusPassrewards = new List<PassRewardData>();
        public bool UseBonusIcon => bonusIcon != null;
        public List<IItemReward> BonusPassrewards => bonusPassrewards.ConvertAll(x => (IItemReward)x);
     
    }
}
