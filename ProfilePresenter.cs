using System;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Profile
{
   public enum ProfileEventType
    {
        ProfileLoaded,
        ProfileNameChanged,
        ProfileAvatarChanged,
        ProfileDataChanged
    }
    
    public class ProfilePresenter
    {
        private const string PROFILE_DATA_KEY = "player_profile_data";
        
        private ProfileModel _currentProfile;
        private readonly IAvatarPresenter _avatarPresenter;
        private readonly ISaveSystem _saveSystem;
        private readonly IEventService _eventService;
        
        private string _defaultPlayerName = "Player";
        private int _defaultAvatarId = 0;
        
        public ProfileModel CurrentProfile => _currentProfile;
        
        public ProfilePresenter(
            ISaveSystem saveSystem, 
            IEventService eventService, 
            IAvatarPresenter avatarPresenter,
            string defaultPlayerName = "Player",
            int defaultAvatarId = 0)
        {
            _saveSystem = saveSystem;
            _eventService = eventService;
            _avatarPresenter = avatarPresenter;
            _defaultPlayerName = defaultPlayerName;
            _defaultAvatarId = defaultAvatarId;
            
            Initialize();
        }
        
        private bool Initialize()
        {
            Debug.Log("[ProfilePresenter] Initializing...");
            
            _saveSystem.RegisterKey<ProfileModel>(PROFILE_DATA_KEY, () => _currentProfile);
            _avatarPresenter.Initialize();
            _avatarPresenter.UnlockAllAvatars();

            LoadProfile();
            Debug.Log("[ProfilePresenter] Initialized successfully.");
            return true;
        }
        
        private void LoadProfile()
        {
            var savedProfile = _saveSystem.Load<ProfileModel>(PROFILE_DATA_KEY);
            
            if (savedProfile == null)
            {
                _currentProfile = new ProfileModel
                {
                    PlayerName = _defaultPlayerName,
                    AvatarId = _defaultAvatarId,
                    CreationDate = DateTime.Now,
                    LastModified = DateTime.Now
                };
               
                SaveProfile();
            }
            else
            {
                _currentProfile = savedProfile;

                if (_avatarPresenter.GetAvatar(_currentProfile.AvatarId) == null)
                {
                    _currentProfile.AvatarId = _defaultAvatarId;
                    SaveProfile();
                }
            }
            _eventService.Publish<ProfileModel, ProfileEventType>(ProfileEventType.ProfileLoaded, _currentProfile);
        }
        
        private void SaveProfile()
        {
            _currentProfile.UpdateLastModified();
            _saveSystem.Save(PROFILE_DATA_KEY, _currentProfile);
        }

        public bool ChangePlayerName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return false;
   
            if (newName.Length > 20)
                newName = newName.Substring(0, 20);
    
            var oldName = _currentProfile.PlayerName;
            _currentProfile.PlayerName = newName;

            SaveProfile();

            _eventService.Publish<string, ProfileEventType>(ProfileEventType.ProfileNameChanged, newName);
            _eventService.Publish<ProfileModel, ProfileEventType>(ProfileEventType.ProfileDataChanged, _currentProfile);
            
            return true;
        }

        public bool ChangePlayerAvatar(int avatarId)
        {
            var avatar = _avatarPresenter.GetAvatar(avatarId);
            if (avatar == null || !avatar.IsUnlocked)
                return false;

            var oldAvatarId = _currentProfile.AvatarId;
            _currentProfile.AvatarId = avatarId;

            SaveProfile();

            _eventService.Publish<int, ProfileEventType>(ProfileEventType.ProfileAvatarChanged, avatarId);
            _eventService.Publish<ProfileModel, ProfileEventType>(ProfileEventType.ProfileDataChanged, _currentProfile);
            
            return true;
        }

        public void ResetProfile()
        {
            _currentProfile = new ProfileModel
            {
                PlayerName = _defaultPlayerName,
                AvatarId = _defaultAvatarId,
                CreationDate = DateTime.Now,
                LastModified = DateTime.Now
            };
            
            SaveProfile();
   
            _eventService.Publish<ProfileModel, ProfileEventType>(ProfileEventType.ProfileLoaded, _currentProfile);
            _eventService.Publish<ProfileModel, ProfileEventType>(ProfileEventType.ProfileDataChanged, _currentProfile);
        }
 
        public string GetPlayerName()
        {
            return _currentProfile.PlayerName;
        }

        public int GetCurrentAvatarId()
        {
            return _currentProfile.AvatarId;
        }

        public Sprite GetCurrentAvatarSprite()
        {
            return _avatarPresenter.GetAvatarSprite(_currentProfile.AvatarId);
        }
    }
}
