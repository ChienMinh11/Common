using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.Profile
{
    public class ProfilePresenter
    {
        public event Action OnStateChanged;
        public event Action<bool> OnSaveCompleted;

        private ProfileModel _currentProfile;
        private readonly IAvatarPresenter _avatarPresenter;
        private readonly IFramePresenter _framePresenter; // Thêm mới
        private readonly IBadgePresenter _badgePresenter; // Thêm mới
        private readonly IProfileSaveAdapter _saveAdapter;
        
        private IProfileView _boundView;

        private string _tempPlayerName;
        private int _tempAvatarId;
        private int _tempFrameId;  // Thêm mới
        private int _tempBadgeId;  // Thêm mới
        
        private string _originalPlayerName;
        private int _originalAvatarId;
        private int _originalFrameId;  // Thêm mới
        private int _originalBadgeId;  // Thêm mới

        // Cập nhật Constructor nhận thêm Frame và Badge Presenter
        public ProfilePresenter(
            IProfileSaveAdapter saveAdapter, 
            IAvatarPresenter avatarPresenter,
            IFramePresenter framePresenter,
            IBadgePresenter badgePresenter)
        {
            _saveAdapter = saveAdapter;
            _avatarPresenter = avatarPresenter;
            _framePresenter = framePresenter;
            _badgePresenter = badgePresenter;
            Initialize();
        }

        private void Initialize()
        {
            _saveAdapter.RegisterProfileKey(() => _currentProfile);
            _avatarPresenter.Initialize();
            _framePresenter.Initialize(); 
            _badgePresenter.Initialize();
            LoadProfile();
        }

        private void LoadProfile()
        {
            _currentProfile = _saveAdapter.LoadProfile() ?? new ProfileModel();
            var currentAvatar = _avatarPresenter.GetAvatar(_currentProfile.AvatarId);
            if (currentAvatar == null || !currentAvatar.IsUnlocked) _currentProfile.AvatarId = 0;

            var currentFrame = _framePresenter.GetFrame(_currentProfile.FrameId);
            if (currentFrame == null || !currentFrame.IsUnlocked) _currentProfile.FrameId = 0;
   
            if (_currentProfile.BadgeId != -1)
            {
                var currentBadge = _badgePresenter.GetBadge(_currentProfile.BadgeId);
                if (currentBadge == null || !currentBadge.IsUnlocked)
                {
                    _currentProfile.BadgeId = -1;
                }
            }

            _saveAdapter.SaveProfile(_currentProfile);
        }

        public void BindView(IProfileView view)
        {
            UnbindView();
            _boundView = view;
            if (_boundView == null) return;
            LoadProfile(); 
            _tempPlayerName = _currentProfile.PlayerName;
            _originalPlayerName = _tempPlayerName;
    
            _tempAvatarId = _currentProfile.AvatarId;
            _originalAvatarId = _tempAvatarId;

            _tempFrameId = _currentProfile.FrameId;
            _originalFrameId = _tempFrameId;

            _tempBadgeId = _currentProfile.BadgeId;
            _originalBadgeId = _tempBadgeId;

            _boundView.OnTemporaryNameChanged += HandleTemporaryNameChanged;
            _boundView.OnAvatarSelected += HandleAvatarSelected;
            _boundView.OnFrameSelected += HandleFrameSelected; 
            _boundView.OnBadgeSelected += HandleBadgeSelected; 
    
            _avatarPresenter.OnAvatarListUpdated += RefreshAvatarGrid;
            _framePresenter.OnFrameListUpdated += RefreshFrameGrid; 
            _badgePresenter.OnBadgeListUpdated += RefreshBadgeGrid; 

            _boundView.ShowProfileData(
                _tempPlayerName, 
                _avatarPresenter.GetAvatarSprite(_tempAvatarId),
                _framePresenter.GetFramePrefab(_tempFrameId),  
                _badgePresenter.GetBadgePrefab(_tempBadgeId)   
            );
         
            RefreshAvatarGrid();
            RefreshFrameGrid();
            RefreshBadgeGrid();
    
            OnStateChanged?.Invoke();
        }

        public void UnbindView()
        {
            if (_boundView == null) return;

            _boundView.OnTemporaryNameChanged -= HandleTemporaryNameChanged;
            _boundView.OnAvatarSelected -= HandleAvatarSelected;
            _boundView.OnFrameSelected -= HandleFrameSelected;
            _boundView.OnBadgeSelected -= HandleBadgeSelected;

            _avatarPresenter.OnAvatarListUpdated -= RefreshAvatarGrid;
            _framePresenter.OnFrameListUpdated -= RefreshFrameGrid;
            _badgePresenter.OnBadgeListUpdated -= RefreshBadgeGrid;

            _boundView = null;
        }

        public void HandleTemporaryNameChanged(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            _tempPlayerName = newName.Length > 20 ? newName.Substring(0, 20) : newName;
            _boundView.UpdatePlayerNameDisplay(_tempPlayerName);
            OnStateChanged?.Invoke();
        }

        private void HandleAvatarSelected(int avatarId)
        {
            var avatar = _avatarPresenter.GetAvatar(avatarId);
            if (avatar == null || !avatar.IsUnlocked) return;

            _tempAvatarId = avatarId;
            _boundView.UpdateAvatarDisplay(avatarId, _avatarPresenter.GetAvatarSprite(avatarId));
            OnStateChanged?.Invoke();
        }

        private void HandleFrameSelected(int frameId)
        {
            var frame = _framePresenter.GetFrame(frameId);
            if (frame == null || !frame.IsUnlocked) return;

            _tempFrameId = frameId;
            _boundView.UpdateFrameDisplay(frameId, _framePresenter.GetFramePrefab(frameId)); // Truyền Prefab
            OnStateChanged?.Invoke();
        }

        private void HandleBadgeSelected(int badgeId)
        {
           
            if (_tempBadgeId == badgeId)
            {
                _tempBadgeId = -1; 
                _boundView.UpdateBadgeDisplay(_tempBadgeId, null); 
                OnStateChanged?.Invoke();
                return;
            }

            var badge = _badgePresenter.GetBadge(badgeId);
            if (badge == null || !badge.IsUnlocked) return;

            _tempBadgeId = badgeId;
            _boundView.UpdateBadgeDisplay(badgeId, _badgePresenter.GetBadgePrefab(badgeId));
            OnStateChanged?.Invoke();
        }

        public void HandleSaveRequested()
        {
            try 
            {
                _currentProfile.PlayerName = _tempPlayerName;
                _currentProfile.AvatarId = _tempAvatarId;
                _currentProfile.FrameId = _tempFrameId;
                _currentProfile.BadgeId = _tempBadgeId;
                _currentProfile.UpdateLastModified();
                
                _saveAdapter.SaveProfile(_currentProfile);
                
                _originalPlayerName = _tempPlayerName;
                _originalAvatarId = _tempAvatarId;
                _originalFrameId = _tempFrameId;
                _originalBadgeId = _tempBadgeId;
                
                OnStateChanged?.Invoke();
                OnSaveCompleted?.Invoke(true); 
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfilePresenter] Save failed: {ex.Message}");
                OnSaveCompleted?.Invoke(false);
            }
        }

        private void RefreshAvatarGrid()
        {
            if (_boundView == null) return;
            var currentAvatar = _avatarPresenter.GetAvatar(_tempAvatarId);
            if (currentAvatar == null || !currentAvatar.IsUnlocked)
            {
                _tempAvatarId = _currentProfile.AvatarId; 
            }
            var allAvatars = _avatarPresenter.GetAllAvatars();
            var displayDataList = new List<AvatarDisplayData>();
            foreach (var avatar in allAvatars)
            {
                var sprite = _avatarPresenter.GetAvatarSprite(avatar.Id);
                displayDataList.Add(new AvatarDisplayData(avatar.Id, avatar.Name, sprite, avatar.IsUnlocked));
            }
            _boundView.PopulateAvatarGrid(displayDataList);
            _boundView.UpdateAvatarDisplay(_tempAvatarId, _avatarPresenter.GetAvatarSprite(_tempAvatarId));
        }

        private void RefreshFrameGrid()
        {
            if (_boundView == null) return;
            var currentFrame = _framePresenter.GetFrame(_tempFrameId);
            if (currentFrame == null || !currentFrame.IsUnlocked)
            {
                _tempFrameId = _currentProfile.FrameId;
            }
            var allFrames = _framePresenter.GetAllFrames();
            var displayDataList = new List<FrameDisplayData>();
            foreach (var frame in allFrames)
            {
                var sprite = _framePresenter.GetFrameSprite(frame.Id);
                var prefab = _framePresenter.GetFramePrefab(frame.Id);
                displayDataList.Add(new FrameDisplayData(frame.Id, frame.Name, sprite, prefab, frame.IsUnlocked));
            }
            _boundView.PopulateFrameGrid(displayDataList);
            _boundView.UpdateFrameDisplay(_tempFrameId, _framePresenter.GetFramePrefab(_tempFrameId));
        }

        private void RefreshBadgeGrid()
        {
            if (_boundView == null) return;
        
            if (_tempBadgeId != -1)
            {
                var currentBadge = _badgePresenter.GetBadge(_tempBadgeId);
                if (currentBadge == null || !currentBadge.IsUnlocked)
                {
                    _tempBadgeId = _currentProfile.BadgeId; 
                }
            }
            var allBadges = _badgePresenter.GetAllBadges();
            var displayDataList = new List<BadgeDisplayData>();
            foreach (var badge in allBadges)
            {
                var sprite = _badgePresenter.GetBadgeSprite(badge.Id);
                var prefab = _badgePresenter.GetBadgePrefab(badge.Id);
                displayDataList.Add(new BadgeDisplayData(badge.Id, badge.Name, sprite, prefab, badge.IsUnlocked));
            }
            _boundView.PopulateBadgeGrid(displayDataList);
            _boundView.UpdateBadgeDisplay(_tempBadgeId, _badgePresenter.GetBadgePrefab(_tempBadgeId));
        }
        
        public bool EquipAvatarDirect(int avatarId)
        {
            var avatar = _avatarPresenter.GetAvatar(avatarId);
            if (avatar == null || !avatar.IsUnlocked) return false;

            _currentProfile.AvatarId = avatarId;
            _currentProfile.UpdateLastModified();
            _saveAdapter.SaveProfile(_currentProfile);

            if (_boundView != null)
            {
                _tempAvatarId = avatarId;
                _originalAvatarId = avatarId;
                _boundView.UpdateAvatarDisplay(avatarId, _avatarPresenter.GetAvatarSprite(avatarId));
            }
            
            OnStateChanged?.Invoke();
            return true;
        }

        public bool EquipFrameDirect(int frameId)
        {
            var frame = _framePresenter.GetFrame(frameId);
            if (frame == null || !frame.IsUnlocked) return false;

            _currentProfile.FrameId = frameId;
            _currentProfile.UpdateLastModified();
            _saveAdapter.SaveProfile(_currentProfile);

            if (_boundView != null)
            {
                _tempFrameId = frameId;
                _originalFrameId = frameId;
                _boundView.UpdateFrameDisplay(frameId, _framePresenter.GetFramePrefab(frameId));
            }

            OnStateChanged?.Invoke();
            return true;
        }

        public bool EquipBadgeDirect(int badgeId)
        {
            // Nếu badgeId = -1 tức là tháo Badge
            if (badgeId == -1)
            {
                _currentProfile.BadgeId = -1;
                _currentProfile.UpdateLastModified();
                _saveAdapter.SaveProfile(_currentProfile);

                if (_boundView != null)
                {
                    _tempBadgeId = -1;
                    _originalBadgeId = -1;
                    _boundView.UpdateBadgeDisplay(-1, null);
                }
                OnStateChanged?.Invoke();
                return true;
            }

            var badge = _badgePresenter.GetBadge(badgeId);
            if (badge == null || !badge.IsUnlocked) return false;

            _currentProfile.BadgeId = badgeId;
            _currentProfile.UpdateLastModified();
            _saveAdapter.SaveProfile(_currentProfile);

            if (_boundView != null)
            {
                _tempBadgeId = badgeId;
                _originalBadgeId = badgeId;
                _boundView.UpdateBadgeDisplay(badgeId, _badgePresenter.GetBadgePrefab(badgeId));
            }

            OnStateChanged?.Invoke();
            return true;
        }

        public bool HasChanges() 
        {
            return _tempAvatarId != _originalAvatarId || 
                   _tempPlayerName != _originalPlayerName ||
                   _tempFrameId != _originalFrameId ||
                   _tempBadgeId != _originalBadgeId;  
        }
    }
}