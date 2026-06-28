using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "ProfileDatabase", menuName = "CORE/Profile/ProfileDatabase")]
    public class ProfileDatabase : ScriptableObject
    {
        [SerializeField] private AvatarConfig avatarConfig;
       public AvatarConfig AvatarConfig => avatarConfig;
    }
}
