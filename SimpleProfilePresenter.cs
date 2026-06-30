using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Profile
{
    public class SimpleProfilePresenter
    {
        private readonly IProfileService _profileService;
        private readonly IProfileSaveAdapter _saveAdapter;
        private readonly ProfileDatabase _database;
        
        private readonly List<ISimpleProfileView> _simpleViews = new List<ISimpleProfileView>();

        public SimpleProfilePresenter(
            IProfileService profileService, 
            IProfileSaveAdapter saveAdapter, 
            ProfileDatabase database)
        {
            _profileService = profileService;
            _saveAdapter = saveAdapter;
            _database = database;
        }

        public void Initialize()
        {
            _profileService.OnStateChanged += NotifyAllSimpleViews;
        }

        public void CleanUp()
        {
            _profileService.OnStateChanged -= NotifyAllSimpleViews;
            _simpleViews.Clear();
        }

        public void RegisterView(ISimpleProfileView view)
        {
            if (!_simpleViews.Contains(view))
            {
                _simpleViews.Add(view);
                UpdateSingleView(view);
            }
        }

        public void UnregisterView(ISimpleProfileView view)
        {
            _simpleViews.Remove(view);
        }

        private void NotifyAllSimpleViews()
        {
            for (int i = _simpleViews.Count - 1; i >= 0; i--)
            {
                if (_simpleViews[i] == null)
                {
                    _simpleViews.RemoveAt(i);
                    continue;
                }
                UpdateSingleView(_simpleViews[i]);
            }
        }

        private void UpdateSingleView(ISimpleProfileView view)
        {
            var profile = _saveAdapter.LoadProfile() ?? new ProfileModel();
            var avatarData = _database.AvatarConfig.GetAvatarById(profile.AvatarId);
            view.UpdateAvatar(avatarData?.AvatarSprite);
            var frameData = _database.FrameConfig.GetFrameById(profile.FrameId);
            view.UpdateFrame(profile.FrameId, frameData?.FramePrefab);
            var badgeData = _database.BadgeConfig.GetBadgeById(profile.BadgeId);
            view.UpdateBadge(profile.BadgeId, badgeData?.BadgePrefab);
        }
    }
}