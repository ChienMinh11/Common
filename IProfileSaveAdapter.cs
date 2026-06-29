using System;
using System.Collections.Generic;

namespace ChieChie.Profile
{
    public interface IProfileSaveAdapter 
    {
        void RegisterProfileKey(Func<ProfileModel> getProfileCallback);
        void RegisterAvatarsKey(Func<Dictionary<int, AvatarModel>> getAvatarsCallback);
        void RegisterFramesKey(Func<Dictionary<int, FrameModel>> getFramesCallback); // Thêm mới
        void RegisterBadgesKey(Func<Dictionary<int, BadgeModel>> getBadgesCallback); // Thêm mới
        
        ProfileModel LoadProfile();
        void SaveProfile(ProfileModel profile);
        
        Dictionary<int, AvatarModel> LoadAvatars();
        void SaveAvatars(Dictionary<int, AvatarModel> avatars);

        Dictionary<int, FrameModel> LoadFrames(); // Thêm mới
        void SaveFrames(Dictionary<int, FrameModel> frames); // Thêm mới

        Dictionary<int, BadgeModel> LoadBadges(); // Thêm mới
        void SaveBadges(Dictionary<int, BadgeModel> badges); // Thêm mới
    }
}