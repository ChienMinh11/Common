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
        [SerializeField] private bool lockByDefault;
  
        public int Id => id;
        public string DisplayName => displayName;
        public Sprite AvatarSprite => avatarSprite;
        public bool LockByDefault => lockByDefault;
      

        public AvatarModel ToAvatarInfo()
        {
            bool initialUnlocked = lockByDefault;
            return new AvatarModel(id, displayName, "", initialUnlocked);
        }
    }
}