using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "BadgeData", menuName = "CORE/Profile/Badge/BadgeData")]
    public class BadgeData : ScriptableObject
    {
        [Header("Badge Information")]
        [SerializeField] private int id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite badgeIcon;
        [SerializeField] private GameObject badgePrefab;
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private string unlockCondition;

        public int Id => id;
        public string DisplayName => displayName;
        public GameObject BadgePrefab => badgePrefab;
        public Sprite BadgeIcon => badgeIcon;
        public bool UnlockedByDefault => unlockedByDefault;
        public string UnlockCondition => unlockCondition;

        public BadgeModel ToBadgeInfo()
        {
            return new BadgeModel(id, displayName, unlockedByDefault);
        }
    }
}