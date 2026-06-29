using System;
using System.Threading;
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
        private readonly IProfileSaveAdapter _saveAdapter;
        public event Action OnCloseRequested;
        public event Action OnEditNameRequested;
       
        public event Action OnStateChanged
        {
            add => _profilePresenter.OnStateChanged += value;
            remove => _profilePresenter.OnStateChanged -= value;
        }

        public ProfileManager(ProfileDatabase database, IProfileSaveAdapter saveAdapter)
        {
            _database = database;
            _saveAdapter = saveAdapter;
        }
        
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _avatarPresenter = new AvatarPresenter(_saveAdapter, _database.AvatarConfig);
            _profilePresenter = new ProfilePresenter(_saveAdapter, _avatarPresenter);
 
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

        public void RequestClose()
        {
            OnCloseRequested?.Invoke();
        }

        public void ChangeTemporaryName(string newName)
        {
            _profilePresenter.HandleTemporaryNameChanged(newName); 
        }

        public bool HasChanges() => _profilePresenter.HasChanges();
        
        public void HandleSaveRequested() => _profilePresenter.HandleSaveRequested();
    }
}