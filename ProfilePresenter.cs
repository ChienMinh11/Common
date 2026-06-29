using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public class ProfilePresenter
    {
        // Sự kiện thông báo khi trạng thái "Thay đổi tạm thời" trên UI biến động
        public event Action OnStateChanged;

        private ProfileModel _currentProfile;
        private readonly IAvatarPresenter _avatarPresenter;
        private readonly IProfileSaveAdapter _saveAdapter;
        
        private IProfileView _boundView;

        private string _tempPlayerName;
        private int _tempAvatarId;
        private string _originalPlayerName;
        private int _originalAvatarId;

        public ProfilePresenter(IProfileSaveAdapter saveAdapter, IAvatarPresenter avatarPresenter)
        {
            _saveAdapter = saveAdapter;
            _avatarPresenter = avatarPresenter;
            Initialize();
        }

        private void Initialize()
        {
            _saveAdapter.RegisterProfileKey(() => _currentProfile);
            _avatarPresenter.Initialize();
            _avatarPresenter.UnlockAllAvatars();
            LoadProfile();
        }

        private void LoadProfile()
        {
            _currentProfile = _saveAdapter.LoadProfile() ?? new ProfileModel();
            if (_avatarPresenter.GetAvatar(_currentProfile.AvatarId) == null)
            {
                _currentProfile.AvatarId = 0;
                _saveAdapter.SaveProfile(_currentProfile);
            }
        }

        public void BindView(IProfileView view)
        {
            UnbindView();
            _boundView = view;

            if (_boundView == null) return;

            _tempPlayerName = _currentProfile.PlayerName;
            _originalPlayerName = _tempPlayerName;
            _tempAvatarId = _currentProfile.AvatarId;
            _originalAvatarId = _tempAvatarId;

            _boundView.OnTemporaryNameChanged += HandleTemporaryNameChanged;
            _boundView.OnAvatarSelected += HandleAvatarSelected;
            
            _avatarPresenter.OnAvatarListUpdated += RefreshAvatarGrid;
            _avatarPresenter.OnAvatarUnlocked += HandleAvatarUnlocked;

            _boundView.ShowProfileData(_tempPlayerName, _avatarPresenter.GetAvatarSprite(_tempAvatarId));
            RefreshAvatarGrid();
            
            // Kích hoạt cập nhật trạng thái nút bấm ban đầu (Sẽ là false)
            OnStateChanged?.Invoke();
        }

        public void UnbindView()
        {
            if (_boundView == null) return;

            _boundView.OnTemporaryNameChanged -= HandleTemporaryNameChanged;
            _boundView.OnAvatarSelected -= HandleAvatarSelected;

            _avatarPresenter.OnAvatarListUpdated -= RefreshAvatarGrid;
            _avatarPresenter.OnAvatarUnlocked -= HandleAvatarUnlocked;

            _boundView = null;
        }

        private void HandleTemporaryNameChanged(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            _tempPlayerName = newName.Length > 20 ? newName.Substring(0, 20) : newName;
            
            _boundView.UpdatePlayerNameDisplay(_tempPlayerName);
            
            // Bắn sự kiện báo dữ liệu tạm thời đã thay đổi
            OnStateChanged?.Invoke();
        }

        private void HandleAvatarSelected(int avatarId)
        {
            var avatar = _avatarPresenter.GetAvatar(avatarId);
            if (avatar == null || !avatar.IsUnlocked) return;

            _tempAvatarId = avatarId;
            _boundView.UpdateAvatarDisplay(avatarId, _avatarPresenter.GetAvatarSprite(avatarId));
            
            // Bắn sự kiện báo dữ liệu tạm thời đã thay đổi
            OnStateChanged?.Invoke();
        }

        public void HandleSaveRequested()
        {
            _currentProfile.PlayerName = _tempPlayerName;
            _currentProfile.AvatarId = _tempAvatarId;
            _currentProfile.UpdateLastModified();
            
            _saveAdapter.SaveProfile(_currentProfile);
            
            _originalPlayerName = _tempPlayerName;
            _originalAvatarId = _tempAvatarId;
            
            // Lưu xong thì trạng thái thay đổi quay về bằng không -> nút Save tự khóa lại
            OnStateChanged?.Invoke();
        }

        private void HandleAvatarUnlocked(AvatarModel avatar)
        {
            RefreshAvatarGrid();
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

        public bool HasChanges() 
        {
            return _tempAvatarId != _originalAvatarId || _tempPlayerName != _originalPlayerName;
        }
    }
}