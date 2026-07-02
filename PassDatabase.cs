using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "PassDatabase", menuName = "CORE/GamePass/Pass Database")]
    public class PassDatabase : ScriptableObject
    {
        [SerializeField] GamePassSettings gamePassSettings;
        [SerializeField] private List<PassData> passItems = new List<PassData>();
        [SerializeField] private List<PassBonusData > bonusPassItems = new List<PassBonusData>();
        [SerializeField] private List<PassReplaceReward> bonusPassBonusItems = new List<PassReplaceReward>();

        public IReadOnlyList<PassData> PassItems => passItems;
        public IReadOnlyList<PassBonusData> BonusPassItems => bonusPassItems;
        public IReadOnlyList<PassReplaceReward> BonusPassBonusItems => bonusPassBonusItems;
        public GamePassSettings GamePassSettings => gamePassSettings;
    }

    [Serializable]
    public class GamePassSettings
    {
        public string passName;
    }
}
