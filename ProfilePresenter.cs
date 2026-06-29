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
            _avatarPresenter.UnlockAllAvatars();

            _framePresenter.Initialize(); // Khởi tạo Frame
            _framePresenter.UnlockAllFrames();

            _badgePresenter.Initialize(); // Khởi tạo Badge
            _badgePresenter.UnlockAllBadges();

            LoadProfile();
        }

        private void LoadProfile()
        {
            _currentProfile = _saveAdapter.LoadProfile() ?? new ProfileModel();
            
            if (_avatarPresenter.GetAvatar(_currentProfile.AvatarId) == null) _currentProfile.AvatarId = 0;
            if (_framePresenter.GetFrame(_currentProfile.FrameId) == null) _currentProfile.FrameId = 0;
            if (_badgePresenter.GetBadge(_currentProfile.BadgeId) == null) _currentProfile.BadgeId = 0;

            _saveAdapter.SaveProfile(_currentProfile);
        }

        public void BindView(IProfileView view)
        {
            UnbindView();
            _boundView = view;

            if (_boundView == null) return;

            // Gán dữ liệu tạm thời ban đầu
            _tempPlayerName = _currentProfile.PlayerName;
            _originalPlayerName = _tempPlayerName;
            
            _tempAvatarId = _currentProfile.AvatarId;
            _originalAvatarId = _tempAvatarId;

            _tempFrameId = _currentProfile.FrameId;
            _originalFrameId = _tempFrameId;

            _tempBadgeId = _currentProfile.BadgeId;
            _originalBadgeId = _tempBadgeId;

            // Đăng ký sự kiện từ View
            _boundView.OnTemporaryNameChanged += HandleTemporaryNameChanged;
            _boundView.OnAvatarSelected += HandleAvatarSelected;
            _boundView.OnFrameSelected += HandleFrameSelected; // Thêm mới
            _boundView.OnBadgeSelected += HandleBadgeSelected; // Thêm mới
            
            _avatarPresenter.OnAvatarListUpdated += RefreshAvatarGrid;
            _framePresenter.OnFrameListUpdated += RefreshFrameGrid; // Thêm mới
            _badgePresenter.OnBadgeListUpdated += RefreshBadgeGrid; // Thêm mới

            // Hiển thị dữ liệu chính lên khung Profile của View
            _boundView.ShowProfileData(
                _tempPlayerName, 
                _avatarPresenter.GetAvatarSprite(_tempAvatarId),
                _framePresenter.GetFrameSprite(_tempFrameId),
                _badgePresenter.GetBadgeSprite(_tempBadgeId)
            );

            // Đổ dữ liệu vào các Grid lựa chọn danh sách
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

        private void HandleFrameSelected(int frameId) // Thêm mới
        {
            var frame = _framePresenter.GetFrame(frameId);
            if (frame == null || !frame.IsUnlocked) return;

            _tempFrameId = frameId;
            _boundView.UpdateFrameDisplay(frameId, _framePresenter.GetFrameSprite(frameId));
            OnStateChanged?.Invoke();
        }

        private void HandleBadgeSelected(int badgeId) // Thêm mới
        {
            var badge = _badgePresenter.GetBadge(badgeId);
            if (badge == null || !badge.IsUnlocked) return;

            _tempBadgeId = badgeId;
            _boundView.UpdateBadgeDisplay(badgeId, _badgePresenter.GetBadgeSprite(badgeId));
            OnStateChanged?.Invoke();
        }

        public void HandleSaveRequested()
        {
            try 
            {
                _currentProfile.PlayerName = _tempPlayerName;
                _currentProfile.AvatarId = _tempAvatarId;
                _currentProfile.FrameId = _tempFrameId; // Thêm mới
                _currentProfile.BadgeId = _tempBadgeId; // Thêm mới
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

        private void RefreshFrameGrid() // Thêm mới
        {
            if (_boundView == null) return;
            var allFrames = _framePresenter.GetAllFrames();
            var displayDataList = new List<FrameDisplayData>();
            foreach (var frame in allFrames)
            {
                var sprite = _framePresenter.GetFrameSprite(frame.Id);
                displayDataList.Add(new FrameDisplayData(frame.Id, frame.Name, sprite, frame.IsUnlocked));
            }
            _boundView.PopulateFrameGrid(displayDataList);
            _boundView.UpdateFrameDisplay(_tempFrameId, _framePresenter.GetFrameSprite(_tempFrameId));
        }

        private void RefreshBadgeGrid() // Thêm mới
        {
            if (_boundView == null) return;
            var allBadges = _badgePresenter.GetAllBadges();
            var displayDataList = new List<BadgeDisplayData>();
            foreach (var badge in allBadges)
            {
                var sprite = _badgePresenter.GetBadgeSprite(badge.Id);
                displayDataList.Add(new BadgeDisplayData(badge.Id, badge.Name, sprite, badge.IsUnlocked));
            }
            _boundView.PopulateBadgeGrid(displayDataList);
            _boundView.UpdateBadgeDisplay(_tempBadgeId, _badgePresenter.GetBadgeSprite(_tempBadgeId));
        }

        public bool HasChanges() 
        {
            return _tempAvatarId != _originalAvatarId || 
                   _tempPlayerName != _originalPlayerName ||
                   _tempFrameId != _originalFrameId || // Thêm kiểm tra đổi Frame
                   _tempBadgeId != _originalBadgeId;   // Thêm kiểm tra đổi Badge
        }
    }
}