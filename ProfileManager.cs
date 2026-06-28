using System.Threading;
using ChieChie.Core;
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
        private readonly IEventService _eventService;

        public ProfilePresenter ProfilePresenter => _profilePresenter;
        public AvatarPresenter AvatarPresenter => _avatarPresenter;

        public ProfileManager(ProfileDatabase database,IProfileSaveAdapter saveAdapter, IEventService eventService)
        {
            _database = database;
            _saveAdapter = saveAdapter;
            _eventService = eventService;
        }
        
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[ProfileManager] Initializing...");

            _avatarPresenter = new AvatarPresenter(_saveAdapter, _eventService, _database.AvatarConfig);
            _profilePresenter = new ProfilePresenter(_saveAdapter, _eventService, _avatarPresenter);
 
            IsInitialized = true;
            Debug.Log("[ProfileManager] Initialized successfully.");
            return UniTask.FromResult(true);
        }
    }
}