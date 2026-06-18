using UnityEngine;

namespace ChieChie.Booster
{
    public abstract class BoosterSetting : ScriptableObject
    {
        [Header("Resource Binding")]
        [SerializeField] private string boosterId; 
        public string BoosterId => boosterId;

        [Header("Booster Configs")]
        [SerializeField] private string boosterName;
        [SerializeField] private string description;
        [SerializeField] private BoosterType boosterType;
        [SerializeField] private GameObject behaviorPrefab;
        [SerializeField] private int requiredLevel;
        [SerializeField] private string floatingMessage;
        [SerializeField] private long cost = 1;

        // Getters
        public string BoosterName => boosterName;
        public string Description => description;
        public BoosterType BoosterType => boosterType;
        public GameObject BehaviorPrefab => behaviorPrefab;
        public int RequiredLevel => requiredLevel;
        public string FloatingMessage => floatingMessage;
        public long Cost => cost; 

        public abstract void Initialise();
    }
}