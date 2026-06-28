using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IProfileSaveAdapter 
    {
        void RegisterProfileKey(Func<ProfileModel> getProfileCallback);
        void RegisterAvatarsKey(Func<Dictionary<int, AvatarModel>> getAvatarsCallback);
        
        ProfileModel LoadProfile();
        void SaveProfile(ProfileModel profile);
        
        Dictionary<int, AvatarModel> LoadAvatars();
        void SaveAvatars(Dictionary<int, AvatarModel> avatars);
    }
}
