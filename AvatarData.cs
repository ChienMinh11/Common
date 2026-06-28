using UnityEngine;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "AvatarData", menuName = "CORE/Profile/Avatar/AvatarData")]
    public class AvatarData : ScriptableObject
    {
        [Header("Avatar Information")]
        [SerializeField] private int id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite avatarSprite;
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private string unlockCondition;

        public int Id => id;
        public string DisplayName => displayName;
        public Sprite AvatarSprite => avatarSprite;
        public bool UnlockedByDefault => unlockedByDefault;
        public string UnlockCondition => unlockCondition;

        public AvatarModel ToAvatarInfo()
        {
            return new AvatarModel(id, displayName, "", UnlockedByDefault, unlockCondition);
        }
    }
}