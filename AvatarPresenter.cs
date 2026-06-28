using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Profile
{
    public enum AvatarEventType
    {
        AvatarChanged,
        AvatarListUpdated,
        AvatarUnlocked
    }
    
    public class AvatarPresenter : IAvatarPresenter
    {
        public bool IsInitialized { get; private set; }
        
        private const string AVATARS_KEY = "player_avatars";
        
        private Dictionary<int, AvatarModel> _avatars = new Dictionary<int, AvatarModel>();
        private Dictionary<int, Sprite> _avatarSprites = new Dictionary<int, Sprite>();
        
        private readonly ISaveSystem _saveSystem;
        private readonly IEventService _eventService;
        private readonly AvatarDatabase _avatarDatabase;
        
        public AvatarPresenter(
            ISaveSystem saveSystem, 
            IEventService eventService, 
            AvatarDatabase avatarDatabase)
        {
            _saveSystem = saveSystem;
            _eventService = eventService;
            _avatarDatabase = avatarDatabase;
        }
        
        public bool Initialize()
        {
            Debug.Log("[AvatarPresenter] Initializing...");
            
            if (_avatarDatabase == null)
            {
                Debug.LogError("[AvatarPresenter] Avatar database is not assigned!");
                return false;
            }
            
            _saveSystem.RegisterKey<Dictionary<int, AvatarModel>>(AVATARS_KEY, () => _avatars);
        
            LoadAvatars();
            
            IsInitialized = true;
            Debug.Log("[AvatarPresenter] Initialized successfully.");
            return true;
        }
        
        private void LoadAvatars()
        {
            var savedAvatars = _saveSystem.Load<Dictionary<int, AvatarModel>>(AVATARS_KEY);
            
            if (savedAvatars == null || savedAvatars.Count == 0)
            {
                _avatars = _avatarDatabase.GetDefaultAvatarInfoDictionary();
                _saveSystem.Save(AVATARS_KEY, _avatars);
            }
            else
            {
                _avatars = savedAvatars;
                foreach (var avatarData in _avatarDatabase.Avatars)
                {
                    if (!_avatars.ContainsKey(avatarData.Id))
                    {
                        AvatarModel newAvatar = avatarData.ToAvatarInfo();
                        _avatars[avatarData.Id] = newAvatar;
                    }
                }
                
                _saveSystem.Save(AVATARS_KEY, _avatars);
            }
            
            foreach (var avatarData in _avatarDatabase.Avatars)
            {
                if (avatarData.AvatarSprite != null)
                {
                    _avatarSprites[avatarData.Id] = avatarData.AvatarSprite;
                }
            }
            
            _eventService.PublishEvent<AvatarEventType>(AvatarEventType.AvatarListUpdated);
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
                _saveSystem.Save(AVATARS_KEY, _avatars);
                _eventService.Publish<AvatarModel, AvatarEventType>(AvatarEventType.AvatarUnlocked, avatar);
                return true;
            }
            return false;
        }

        public void AddAvatar(AvatarModel newAvatar)
        {
            if (!_avatars.ContainsKey(newAvatar.Id))
            {
                _avatars[newAvatar.Id] = newAvatar;
                _saveSystem.Save(AVATARS_KEY, _avatars);
                _eventService.PublishEvent<AvatarEventType>(AvatarEventType.AvatarListUpdated);
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
                    _eventService.Publish<AvatarModel, AvatarEventType>(AvatarEventType.AvatarUnlocked, avatar);
                }
            }
    
            if (anyUnlocked)
            {
                _saveSystem.Save(AVATARS_KEY, _avatars);
                _eventService.PublishEvent<AvatarEventType>(AvatarEventType.AvatarListUpdated);
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
