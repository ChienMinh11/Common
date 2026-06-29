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

        // Quản lý việc đăng ký View tập trung tại đây
        public void RegisterView(IProfileView view)
        {
            _profilePresenter.BindView(view);
        }

        public void UnregisterView()
        {
            _profilePresenter.UnbindView();
        }

        public ProfilePresenter GetProfilePresenter() => _profilePresenter;
        public IAvatarPresenter GetAvatarPresenter() => _avatarPresenter;
    }
}