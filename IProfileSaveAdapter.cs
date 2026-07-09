using System;
using System.Collections.Generic;

namespace ChieChie.Profile
{
    [Serializable]
    public class ProfileSaveData
    {
        public ProfileModel profile;
        public Dictionary<int, AvatarModel> avatars = new Dictionary<int, AvatarModel>();
        public Dictionary<int, FrameModel> frames = new Dictionary<int, FrameModel>();
        public Dictionary<int, BadgeModel> badges = new Dictionary<int, BadgeModel>();
    }
    public interface IProfileSaveAdapter 
    {
        ProfileSaveData LoadData();
        void SaveData(ProfileSaveData data);
    }
}