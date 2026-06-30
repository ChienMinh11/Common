using System;

namespace ChieChie.Constracts
{
    public interface IProfileService
    {
        event Action OnStateChanged;
        event Action OnCloseRequested; 
        event Action OnEditNameRequested;
        event Action<bool> OnSaveCompleted;

        void RegisterView(IProfileView view);
        void UnregisterView(IProfileView view);
        bool HasChanges();
        void HandleSaveRequested();
        void RequestClose();
        void ChangeTemporaryName(string newName);
      
        bool UnlockAvatar(int avatarId);
        bool LockAvatar(int avatarId);
        bool IsAvatarUnlocked(int avatarId);

        bool UnlockFrame(int frameId);
        bool LockFrame(int frameId);
        bool IsFrameUnlocked(int frameId);

        bool UnlockBadge(int badgeId);
        bool LockBadge(int badgeId);
        bool IsBadgeUnlocked(int badgeId);
        
        bool EquipAvatar(int avatarId);
        bool EquipFrame(int frameId);
        bool EquipBadge(int badgeId);
        
        
    }
}
