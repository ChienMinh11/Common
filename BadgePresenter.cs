using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public class BadgePresenter : IBadgePresenter
    {
        public event Action OnBadgeListUpdated;
        public event Action<BadgeModel> OnBadgeUnlocked;

        public bool IsInitialized { get; private set; }
        
        private Dictionary<int, BadgeModel> _badges = new Dictionary<int, BadgeModel>();
        private Dictionary<int, Sprite> _badgeIcons = new Dictionary<int, Sprite>();
        private Dictionary<int, GameObject> _badgePrefabs = new Dictionary<int, GameObject>();
        
        private readonly IProfileSaveAdapter _saveAdapter;
        private readonly BadgeConfig _badgeConfig;
        
        public BadgePresenter(IProfileSaveAdapter saveAdapter, BadgeConfig badgeConfig)
        {
            _saveAdapter = saveAdapter;
            _badgeConfig = badgeConfig;
        }
        
        public bool Initialize()
        {
            if (_badgeConfig == null)
            {
                Debug.LogError("[BadgePresenter] Badge config is not assigned!");
                return false;
            }
            
            LoadBadges();
            IsInitialized = true;
            return true;
        }
        
        private void LoadBadges()
        {
            var rootData = _saveAdapter.LoadData();
            var savedBadges = rootData.badges;
    
            if (savedBadges == null || savedBadges.Count == 0)
            {
                _badges = _badgeConfig.GetDefaultBadgeInfoDictionary();
                rootData.badges = _badges;
                _saveAdapter.SaveData(rootData);
            }
            else
            {
                _badges = savedBadges;
                foreach (var badgeData in _badgeConfig.Badges)
                {
                    if (!_badges.ContainsKey(badgeData.Id))
                    {
                        var newInfo = badgeData.ToBadgeInfo();
                        newInfo.IsUnlocked = !badgeData.LockByDefault; 
                        _badges[badgeData.Id] = newInfo;
                    }
                }
                _saveAdapter.SaveData(rootData);
            }
            
            foreach (var badgeData in _badgeConfig.Badges)
            {
                if (badgeData.BadgeIcon != null) _badgeIcons[badgeData.Id] = badgeData.BadgeIcon;
                if (badgeData.BadgePrefab != null) _badgePrefabs[badgeData.Id] = badgeData.BadgePrefab;
            }
            
            OnBadgeListUpdated?.Invoke();
        }
        
        public List<BadgeModel> GetAllBadges() => new List<BadgeModel>(_badges.Values);
        public BadgeModel GetBadge(int badgeId) => _badges.TryGetValue(badgeId, out var badge) ? badge : null;
        public Sprite GetBadgeSprite(int badgeId) => _badgeIcons.TryGetValue(badgeId, out var sprite) ? sprite : null;
        public GameObject GetBadgePrefab(int frameId) => _badgePrefabs.TryGetValue(frameId, out var prefab) ? prefab : null;

        public bool LockBadge(int badgeId)
        {
            if (_badges.TryGetValue(badgeId, out var badge))
            {
                if (!badge.IsUnlocked) return false; 
        
                badge.IsUnlocked = false;
                
                var rootData = _saveAdapter.LoadData();
                rootData.badges = _badges;

                if (rootData.profile != null && rootData.profile.BadgeId == badgeId)
                {
                    rootData.profile.BadgeId = -1;
                }

                _saveAdapter.SaveData(rootData);
                OnBadgeListUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public bool UnlockBadge(int badgeId)
        {
            if (_badges.TryGetValue(badgeId, out var badge))
            {
                if (badge.IsUnlocked) return false; 
                
                badge.IsUnlocked = true;
                
                var rootData = _saveAdapter.LoadData();
                rootData.badges = _badges;
                _saveAdapter.SaveData(rootData);

                OnBadgeUnlocked?.Invoke(badge);
                return true;
            }
            return false;
        }
        
        public void UnlockAllBadges()
        {
            bool anyUnlocked = false;
            foreach (var badge in _badges.Values)
            {
                if (!badge.IsUnlocked)
                {
                    badge.IsUnlocked = true;
                    anyUnlocked = true;
                    OnBadgeUnlocked?.Invoke(badge);
                }
            }
    
            if (anyUnlocked)
            {
                var rootData = _saveAdapter.LoadData();
                rootData.badges = _badges;
                _saveAdapter.SaveData(rootData);

                OnBadgeListUpdated?.Invoke();
            }
        }

        public bool IsBadgeUnlocked(int badgeId) => _badges.TryGetValue(badgeId, out var badge) && badge.IsUnlocked;
    }
}