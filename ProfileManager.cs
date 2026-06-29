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

        public ProfilePresenter ProfilePresenter => _profilePresenter;
        public AvatarPresenter AvatarPresenter => _avatarPresenter;

        public ProfileManager(ProfileDatabase database, IProfileSaveAdapter saveAdapter)
        {
            _database = database;
            _saveAdapter = saveAdapter;
        }
        
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[ProfileManager] Initializing...");

            // Loại bỏ hoàn toàn _eventService tại đây
            _avatarPresenter = new AvatarPresenter(_saveAdapter, _database.AvatarConfig);
            _profilePresenter = new ProfilePresenter(_saveAdapter, _avatarPresenter);
 
            IsInitialized = true;
            Debug.Log("[ProfileManager] Initialized successfully.");
            return UniTask.FromResult(true);
        }

        public ProfilePresenter GetProfilePresenter()
        {
            return _profilePresenter;
        }

        public IAvatarPresenter GetAvatarPresenter()
        {
            return _avatarPresenter;
        }
    }
}