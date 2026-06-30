using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassBonusItem", menuName = "CORE/GamePass/Pass Bonus Data")]
    public class PassBonusData 
    {
        public int requiredAmount;
        public List<PassRewardData> bonusPassrewards = new List<PassRewardData>();
    }
}
