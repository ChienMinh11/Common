using System;
using System.Collections.Generic;
using ChieChie.Core;
using ChieChie.Profile;
using UnityEngine;

namespace Game.DependencyInjection
{
    public class ProfileSaveAdapter : IProfileSaveAdapter
    {
        private const string PROFILE_DATA_KEY = "player_profile_data";
        private const string AVATARS_KEY = "player_avatars";

        private readonly ISaveSystem _saveSystem;

        public ProfileSaveAdapter(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
        }

        public void RegisterProfileKey(Func<ProfileModel> getProfileCallback)
        {
            _saveSystem.RegisterKey<ProfileModel>(PROFILE_DATA_KEY, getProfileCallback);
        }

        public void RegisterAvatarsKey(Func<Dictionary<int, AvatarModel>> getAvatarsCallback)
        {
            _saveSystem.RegisterKey<Dictionary<int, AvatarModel>>(AVATARS_KEY, getAvatarsCallback);
        }

        public ProfileModel LoadProfile()
        {
            return _saveSystem.Load<ProfileModel>(PROFILE_DATA_KEY);
        }

        public void SaveProfile(ProfileModel profile)
        {
            _saveSystem.Save(PROFILE_DATA_KEY, profile);
        }

        public Dictionary<int, AvatarModel> LoadAvatars()
        {
            return _saveSystem.Load<Dictionary<int, AvatarModel>>(AVATARS_KEY);
        }

        public void SaveAvatars(Dictionary<int, AvatarModel> avatars)
        {
            _saveSystem.Save(AVATARS_KEY, avatars);
        }
    }
}