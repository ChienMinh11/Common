using System;
using UnityEngine;

namespace ChieChie.Profile
{
    public class ProfilePresenter
    {
        public event Action<ProfileModel> OnProfileLoaded;
        public event Action<string> OnProfileNameChanged;
        public event Action<int> OnProfileAvatarChanged;
        public event Action<ProfileModel> OnProfileDataChanged;

        private ProfileModel _currentProfile;
        private readonly IAvatarPresenter _avatarPresenter;
        private readonly IProfileSaveAdapter _saveAdapter;
        
        private string _defaultPlayerName = "Player";
        private int _defaultAvatarId = 0;
        
        public ProfileModel CurrentProfile => _currentProfile;
        
        public ProfilePresenter(
            IProfileSaveAdapter saveAdapter, 
            IAvatarPresenter avatarPresenter,
            string defaultPlayerName = "Player",
            int defaultAvatarId = 0)
        {
            _saveAdapter = saveAdapter;
            _avatarPresenter = avatarPresenter;
            _defaultPlayerName = defaultPlayerName;
            _defaultAvatarId = defaultAvatarId;
            
            Initialize();
        }
        
        private bool Initialize()
        {
            Debug.Log("[ProfilePresenter] Initializing...");
            
            _saveAdapter.RegisterProfileKey(() => _currentProfile);
            _avatarPresenter.Initialize();
            _avatarPresenter.UnlockAllAvatars();

            LoadProfile();
            Debug.Log("[ProfilePresenter] Initialized successfully.");
            return true;
        }
        
        private void LoadProfile()
        {
            var savedProfile = _saveAdapter.LoadProfile();
            
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
            OnProfileLoaded?.Invoke(_currentProfile);
        }
        
        private void SaveProfile()
        {
            _currentProfile.UpdateLastModified();
            _saveAdapter.SaveProfile(_currentProfile);
        }

        public bool ChangePlayerName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return false;
   
            if (newName.Length > 20)
                newName = newName.Substring(0, 20);
    
            _currentProfile.PlayerName = newName;

            SaveProfile();

            OnProfileNameChanged?.Invoke(newName);
            OnProfileDataChanged?.Invoke(_currentProfile);
            
            return true;
        }

        public bool ChangePlayerAvatar(int avatarId)
        {
            var avatar = _avatarPresenter.GetAvatar(avatarId);
            if (avatar == null || !avatar.IsUnlocked)
                return false;

            _currentProfile.AvatarId = avatarId;

            SaveProfile();

            OnProfileAvatarChanged?.Invoke(avatarId);
            OnProfileDataChanged?.Invoke(_currentProfile);
            
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
   
            OnProfileLoaded?.Invoke(_currentProfile);
            OnProfileDataChanged?.Invoke(_currentProfile);
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