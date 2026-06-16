using UnityEngine;

namespace ChieChie.Core
{
    [CreateAssetMenu(fileName = "TimeServiceSettings", menuName = "CORE/Configs/TimeServiceSettings")]
    public class TimeServiceSettings : ScriptableObject
    {
        [Header("Testing Options")]
        [Tooltip("WARNING: Only enable this in testing environments!")]
        public bool useLocalTimeForTesting;
        
        [Header("General Settings")]
        public int serverTimeoutSeconds = 5;
        public float syncIntervalMinutes = 30f;
        public bool debugMode;
    }
}