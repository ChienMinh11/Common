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
    }
}
