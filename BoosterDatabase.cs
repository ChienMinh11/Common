using UnityEngine;

namespace ChieChie.Booster
{
    [CreateAssetMenu(fileName = "Booster Database", menuName = "CORE/Configs/Booster Database")]
    public class BoosterDatabase : ScriptableObject
    {
        [SerializeField] BoosterSetting[] boosters;
        public BoosterSetting[] Boosters => boosters;
    }
}
