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
        
        public AvatarPresenter(
            IProfileSaveAdapter saveAdapter, 
            AvatarConfig avatarConfig)
        {
            _saveAdapter = saveAdapter;
            _avatarConfig = avatarConfig;
        }
        
        public bool Initialize()
        {
            Debug.Log("[AvatarPresenter] Initializing...");
            
            if (_avatarConfig == null)
            {
                Debug.LogError("[AvatarPresenter] Avatar config is not assigned!");
                return false;
            }
            
            _saveAdapter.RegisterAvatarsKey(() => _avatars);
        
            LoadAvatars();
            
            IsInitialized = true;
            Debug.Log("[AvatarPresenter] Initialized successfully.");
            return true;
        }
        
        private void LoadAvatars()
        {
            var savedAvatars = _saveAdapter.LoadAvatars();
            
            if (savedAvatars == null || savedAvatars.Count == 0)
            {
                _avatars = _avatarConfig.GetDefaultAvatarInfoDictionary();
                _saveAdapter.SaveAvatars(_avatars);
            }
            else
            {
                _avatars = savedAvatars;
                foreach (var avatarData in _avatarConfig.Avatars)
                {
                    if (!_avatars.ContainsKey(avatarData.Id))
                    {
                        AvatarModel newAvatar = avatarData.ToAvatarInfo();
                        _avatars[avatarData.Id] = newAvatar;
                    }
                }
                
                _saveAdapter.SaveAvatars(_avatars);
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
        
        public List<AvatarModel> GetAllAvatars()
        {
            var avatarList = new List<AvatarModel>();
            foreach (var avatar in _avatars.Values)
            {
                avatarList.Add(avatar);
            }
            return avatarList;
        }
        
        public List<AvatarModel> GetUnlockedAvatars()
        {
            var unlockedAvatars = new List<AvatarModel>();
            foreach (var avatar in _avatars.Values)
            {
                if (avatar.IsUnlocked)
                {
                    unlockedAvatars.Add(avatar);
                }
            }
            return unlockedAvatars;
        }
  
        public AvatarModel GetAvatar(int avatarId)
        {
            if (_avatars.TryGetValue(avatarId, out var avatar))
            {
                return avatar;
            }
            return null;
        }
     
        public Sprite GetAvatarSprite(int avatarId)
        {
            if (_avatarSprites.TryGetValue(avatarId, out var sprite))
            {
                return sprite;
            }
            return null;
        }

        public bool UnlockAvatar(int avatarId)
        {
            if (_avatars.TryGetValue(avatarId, out var avatar))
            {
                if (avatar.IsUnlocked) return false; 
                
                avatar.IsUnlocked = true;
                _saveAdapter.SaveAvatars(_avatars);
                OnAvatarUnlocked?.Invoke(avatar);
                return true;
            }
            return false;
        }

        public void AddAvatar(AvatarModel newAvatar)
        {
            if (!_avatars.ContainsKey(newAvatar.Id))
            {
                _avatars[newAvatar.Id] = newAvatar;
                _saveAdapter.SaveAvatars(_avatars);
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
                _saveAdapter.SaveAvatars(_avatars);
                OnAvatarListUpdated?.Invoke();
            }
        }

        public bool IsAvatarUnlocked(int avatarId)
        {
            if (_avatars.TryGetValue(avatarId, out var avatar))
            {
                return avatar.IsUnlocked;
            }
            return false;
        }
    }
}