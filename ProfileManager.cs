using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Profile
{

    
    public class ProfileManager:IProfileService
    { 
        public bool IsInitialized { get; private set; }
        [SerializeField] private AvatarDatabase avatarDatabase;
        private ProfilePresenter _profilePresenter;
        private AvatarPresenter _avatarPresenter;
  
        private ISaveSystem _saveSystem;
        private IEventService _eventService;

        public ProfilePresenter ProfilePresenter => _profilePresenter;
        public AvatarPresenter AvatarPresenter => _avatarPresenter;

        public ProfileManager(ISaveSystem saveSystem, IEventService eventService)
        {
            _saveSystem = saveSystem;
            _eventService = eventService;
        }
        
      
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[ProfileManager] Initializing...");

            _avatarPresenter = new AvatarPresenter(_saveSystem, _eventService, avatarDatabase);
            _profilePresenter = new ProfilePresenter(_saveSystem, _eventService, _avatarPresenter);
 
            IsInitialized = true;
            Debug.Log("[ProfileManager] Initialized successfully.");
            return UniTask.FromResult(true);
        }
    
    }
}