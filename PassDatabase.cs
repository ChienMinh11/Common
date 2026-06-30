using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "PassDatabase", menuName = "CORE/GamePass/Pass Reward Database")]
    public class PassDatabase : ScriptableObject
    {
        [SerializeField] private List<PassData> passItems = new List<PassData>();
        [SerializeField] private List<PassBonusData > bonusPassItems = new List<PassBonusData>();
        public IReadOnlyList<PassData> PassItems => passItems;
        public IReadOnlyList<PassBonusData> BonusPassItems => bonusPassItems;
    }
}
