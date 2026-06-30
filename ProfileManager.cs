using System;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Profile
{
    public class ProfileManager : IProfileService
    { 
        public bool IsInitialized { get; private set; }
        private ProfileDatabase _database;
        private ProfilePresenter _profilePresenter;
        private AvatarPresenter _avatarPresenter;
        private FramePresenter _framePresenter; // Thêm mới
        private BadgePresenter _badgePresenter; // Thêm mới
        private readonly IProfileSaveAdapter _saveAdapter;
        
        public event Action OnCloseRequested;
        public event Action OnEditNameRequested;
       
        public event Action OnStateChanged
        {
            add => _profilePresenter.OnStateChanged += value;
            remove => _profilePresenter.OnStateChanged -= value;
        }
        public event Action<bool> OnSaveCompleted
        {
            add => _profilePresenter.OnSaveCompleted += value;
            remove => _profilePresenter.OnSaveCompleted -= value;
        }

        public ProfileManager(ProfileDatabase database, IProfileSaveAdapter saveAdapter)
        {
            _database = database;
            _saveAdapter = saveAdapter;
        }
        
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _avatarPresenter = new AvatarPresenter(_saveAdapter, _database.AvatarConfig);
            _framePresenter = new FramePresenter(_saveAdapter, _database.FrameConfig); // Khởi tạo Frame Presenter
            _badgePresenter = new BadgePresenter(_saveAdapter, _database.BadgeConfig); // Khởi tạo Badge Presenter
            
            // Truyền tất cả Presenter vào ProfilePresenter để kết nối dữ liệu
            _profilePresenter = new ProfilePresenter(_saveAdapter, _avatarPresenter, _framePresenter, _badgePresenter);
 
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        public void RegisterView(IProfileView view)
        {
            _profilePresenter.BindView(view);
            view.OnCloseRequested += HandleCloseRequested;
            view.OnEditNameRequested += HandleEditNameRequested;
        }

        public void UnregisterView(IProfileView view)
        {
            if (view != null)
            {
                view.OnCloseRequested -= HandleCloseRequested;
                view.OnEditNameRequested -= HandleEditNameRequested;
            }
            _profilePresenter.UnbindView();
        }
        
        private void HandleCloseRequested() => OnCloseRequested?.Invoke();
        private void HandleEditNameRequested() => OnEditNameRequested?.Invoke();
        public void RequestClose() => OnCloseRequested?.Invoke();
        public void ChangeTemporaryName(string newName) => _profilePresenter.HandleTemporaryNameChanged(newName);
        public bool HasChanges() => _profilePresenter.HasChanges();
        public void HandleSaveRequested() => _profilePresenter.HandleSaveRequested();
        
        // --- IMPLEMENTATION LOCK/UNLOCK FOR AVATAR ---
        public bool UnlockAvatar(int avatarId) => _avatarPresenter.UnlockAvatar(avatarId);
        public bool LockAvatar(int avatarId) => _avatarPresenter.LockAvatar(avatarId);
        public bool IsAvatarUnlocked(int avatarId) => _avatarPresenter.IsAvatarUnlocked(avatarId);

        // --- IMPLEMENTATION LOCK/UNLOCK FOR FRAME ---
        public bool UnlockFrame(int frameId) => _framePresenter.UnlockFrame(frameId);
        public bool LockFrame(int frameId) => _framePresenter.LockFrame(frameId);
        public bool IsFrameUnlocked(int frameId) => _framePresenter.IsFrameUnlocked(frameId);

        // --- IMPLEMENTATION LOCK/UNLOCK FOR BADGE ---
        public bool UnlockBadge(int badgeId) => _badgePresenter.UnlockBadge(badgeId);
        public bool LockBadge(int badgeId) => _badgePresenter.LockBadge(badgeId);
        public bool IsBadgeUnlocked(int badgeId) => _badgePresenter.IsBadgeUnlocked(badgeId);
        
        // --- THÊM MỚI: IMPLEMENTATION EQUIP FOR AVATAR, FRAME, BADGE ---
        public bool EquipAvatar(int avatarId) => _profilePresenter.EquipAvatarDirect(avatarId);
        public bool EquipFrame(int frameId) => _profilePresenter.EquipFrameDirect(frameId);
        public bool EquipBadge(int badgeId) => _profilePresenter.EquipBadgeDirect(badgeId);
    }
}