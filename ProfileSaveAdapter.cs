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
        private const string FRAMES_KEY = "player_frames";   // Thêm mới
        private const string BADGES_KEY = "player_badges";   // Thêm mới

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

        public void RegisterFramesKey(Func<Dictionary<int, FrameModel>> getFramesCallback)
        {
            _saveSystem.RegisterKey<Dictionary<int, FrameModel>>(FRAMES_KEY, getFramesCallback);
        }

        public void RegisterBadgesKey(Func<Dictionary<int, BadgeModel>> getBadgesCallback)
        {
            _saveSystem.RegisterKey<Dictionary<int, BadgeModel>>(BADGES_KEY, getBadgesCallback);
        }

        public ProfileModel LoadProfile() => _saveSystem.Load<ProfileModel>(PROFILE_DATA_KEY);
        public void SaveProfile(ProfileModel profile) => _saveSystem.Save(PROFILE_DATA_KEY, profile);

        public Dictionary<int, AvatarModel> LoadAvatars() => _saveSystem.Load<Dictionary<int, AvatarModel>>(AVATARS_KEY);
        public void SaveAvatars(Dictionary<int, AvatarModel> avatars) => _saveSystem.Save(AVATARS_KEY, avatars);

        // Hoàn thiện các hàm cho Frame và Badge
        public Dictionary<int, FrameModel> LoadFrames()
        {
            return _saveSystem.Load<Dictionary<int, FrameModel>>(FRAMES_KEY);
        }

        public void SaveFrames(Dictionary<int, FrameModel> frames)
        {
            _saveSystem.Save(FRAMES_KEY, frames);
        }

        public Dictionary<int, BadgeModel> LoadBadges()
        {
            return _saveSystem.Load<Dictionary<int, BadgeModel>>(BADGES_KEY);
        }

        public void SaveBadges(Dictionary<int, BadgeModel> badges)
        {
            _saveSystem.Save(BADGES_KEY, badges);
        }
    }
}