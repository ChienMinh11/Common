using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassBonusItem", menuName = "CORE/GamePass/Pass Bonus Data")]
    public class PassBonusData: ScriptableObject
    {
        public int index;
        public int requiredAmount;
        public GameObject bonusIcon;
        public List<PassRewardData> bonusPassrewards = new List<PassRewardData>();
        public bool UseBonusIcon => bonusIcon != null;
    }
}
