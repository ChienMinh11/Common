using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "ProfileDatabase", menuName = "CORE/Profile/ProfileDatabase")]
    public class ProfileDatabase : ScriptableObject
    {
        [SerializeField] private AvatarConfig avatarConfig;
        [SerializeField] private FrameConfig frameConfig;
        [SerializeField] private BadgeConfig badgeConfig;
       public AvatarConfig AvatarConfig => avatarConfig;
       public FrameConfig FrameConfig => frameConfig;
       public BadgeConfig BadgeConfig => badgeConfig;
    }
}
