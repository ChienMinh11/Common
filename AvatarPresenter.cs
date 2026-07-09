using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public class AvatarPresenter : IAvatarPresenter
    {
        public event Action OnAvatarListUpdated;
        public event Action<AvatarModel> OnAvatarUnlocked;

        public bool IsInitialized { get; private set; }
        
        private Dictionary<int, AvatarModel> _avatars = new Dictionary<int, AvatarModel>();
        private Dictionary<int, Sprite> _avatarSprites = new Dictionary<int, Sprite>();
        
        private readonly IProfileSaveAdapter _saveAdapter;
        private readonly AvatarConfig _avatarConfig;
        
        public AvatarPresenter(IProfileSaveAdapter saveAdapter, AvatarConfig avatarConfig)
        {
            _saveAdapter = saveAdapter;
            _avatarConfig = avatarConfig;
        }
        
        public bool Initialize()
        {
            if (_avatarConfig == null)
            {
                Debug.LogError("[AvatarPresenter] Avatar config is not assigned!");
                return false;
            }
        
            LoadAvatars();
            IsInitialized = true;
            return true;
        }
        
        private void LoadAvatars()
        {
            var rootData = _saveAdapter.LoadData();
            var savedAvatars = rootData.avatars;
    
            if (savedAvatars == null || savedAvatars.Count == 0)
            {
                _avatars = _avatarConfig.GetDefaultAvatarInfoDictionary();
                rootData.avatars = _avatars;
                _saveAdapter.SaveData(rootData);
            }
            else
            {
                _avatars = savedAvatars;
                foreach (var avatarData in _avatarConfig.Avatars)
                {
                    if (!_avatars.ContainsKey(avatarData.Id))
                    {
                        var newAvatarInfo = avatarData.ToAvatarInfo();
                        newAvatarInfo.IsUnlocked = !avatarData.LockByDefault; 
                        _avatars[avatarData.Id] = newAvatarInfo;
                    }
                }
                _saveAdapter.SaveData(rootData);
            }

            foreach (var avatarData in _avatarConfig.Avatars)
            {
                if (avatarData.AvatarSprite != null)
                {
                    _avatarSprites[avatarData.Id] = avatarData.AvatarSprite;
                }
            }
            
            OnAvatarListUpdated?.Invoke();
        }
        
        public List<AvatarModel> GetAllAvatars() => new List<AvatarModel>(_avatars.Values);
        
        public List<AvatarModel> GetUnlockedAvatars()
        {
            var unlocked = new List<AvatarModel>();
            foreach (var avatar in _avatars.Values)
            {
                if (avatar.IsUnlocked) unlocked.Add(avatar);
            }
            return unlocked;
        }
  
        public AvatarModel GetAvatar(int avatarId) => _avatars.TryGetValue(avatarId, out var avatar) ? avatar : null;
        public Sprite GetAvatarSprite(int avatarId) => _avatarSprites.TryGetValue(avatarId, out var sprite) ? sprite : null;

        public bool UnlockAvatar(int avatarId)
        {
            if (_avatars.TryGetValue(avatarId, out var avatar))
            {
                if (avatar.IsUnlocked) return false; 
                
                avatar.IsUnlocked = true;
                
                var rootData = _saveAdapter.LoadData();
                rootData.avatars = _avatars;
                _saveAdapter.SaveData(rootData);
                
                OnAvatarUnlocked?.Invoke(avatar);
                return true;
            }
            return false;
        }

        public bool LockAvatar(int avatarId)
        {
            if (_avatars.TryGetValue(avatarId, out var avatar))
            {
                if (!avatar.IsUnlocked) return false; 
                avatar.IsUnlocked = false;
                
                var rootData = _saveAdapter.LoadData();
                rootData.avatars = _avatars;

                if (rootData.profile != null && rootData.profile.AvatarId == avatarId)
                {
                    int fallbackId = 0;
                    foreach (var pair in _avatars)
                    {
                        if (pair.Value.IsUnlocked)
                        {
                            fallbackId = pair.Key;
                            break;
                        }
                    }
                    rootData.profile.AvatarId = fallbackId;
                }
                
                _saveAdapter.SaveData(rootData);
                OnAvatarListUpdated?.Invoke(); 
                return true;
            }
            return false;
        }

        public void AddAvatar(AvatarModel newAvatar)
        {
            if (!_avatars.ContainsKey(newAvatar.Id))
            {
                _avatars[newAvatar.Id] = newAvatar;
                
                var rootData = _saveAdapter.LoadData();
                rootData.avatars = _avatars;
                _saveAdapter.SaveData(rootData);
                
                OnAvatarListUpdated?.Invoke();
            }
        }
        
        public void UnlockAllAvatars()
        {
            bool anyUnlocked = false;
            foreach (var avatar in _avatars.Values)
            {
                if (!avatar.IsUnlocked)
                {
                    avatar.IsUnlocked = true;
                    anyUnlocked = true;
                    OnAvatarUnlocked?.Invoke(avatar);
                }
            }
    
            if (anyUnlocked)
            {
                var rootData = _saveAdapter.LoadData();
                rootData.avatars = _avatars;
                _saveAdapter.SaveData(rootData);
                
                OnAvatarListUpdated?.Invoke();
            }
        }

        public bool IsAvatarUnlocked(int avatarId) => _avatars.TryGetValue(avatarId, out var avatar) && avatar.IsUnlocked;
    }
}