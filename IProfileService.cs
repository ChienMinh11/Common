using System;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IProfileService
    {
        event Action OnStateChanged;
        void RegisterView(IProfileView view);
        void UnregisterView();
        bool HasChanges();
        void HandleSaveRequested();
    }
}
