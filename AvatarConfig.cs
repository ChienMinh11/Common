using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "AvatarConfig", menuName = "CORE/Profile/Avatar/AvatarConfig")]
    public class AvatarConfig : ScriptableObject
    {
        [SerializeField] private List<AvatarData> avatars = new List<AvatarData>();
        [SerializeField] private AvatarData defaultAvatar;

        public List<AvatarData> Avatars => avatars;

        public AvatarData DefaultAvatar
        {
            get => defaultAvatar;
            set => defaultAvatar = value;
        }

        public AvatarData GetAvatarById(int id)
        {
            return avatars.Find(avatar => avatar.Id == id);
        }

        public Dictionary<int, AvatarModel> GetDefaultAvatarInfoDictionary()
        {
            Dictionary<int, AvatarModel> result = new Dictionary<int, AvatarModel>();
            
            foreach (var avatar in avatars)
            {
                AvatarModel avatarModel = avatar.ToAvatarInfo();
                avatarModel.IsUnlocked = !avatar.LockByDefault;
                result[avatarModel.Id] = avatarModel;
            }
            
            return result;
        }
        
        public List<AvatarModel> GetAvatarInfoList()
        {
            List<AvatarModel> result = new List<AvatarModel>();
            
            foreach (var avatar in avatars)
            {
                AvatarModel avatarModel = avatar.ToAvatarInfo();
        
                avatarModel.IsUnlocked = !avatar.LockByDefault;
        
                result.Add(avatarModel);
            }
            
            return result;
        }
    }
}