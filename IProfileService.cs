using System;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IProfileService
    {
        event Action OnStateChanged;
        event Action OnCloseRequested; 
        event Action OnEditNameRequested;

        void RegisterView(IProfileView view);
        void UnregisterView(IProfileView view);
        bool HasChanges();
        void HandleSaveRequested();
        void RequestClose();
        void ChangeTemporaryName(string newName);
    }
}
