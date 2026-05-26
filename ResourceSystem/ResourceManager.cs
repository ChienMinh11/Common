using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework
{
    public class ResourceManager : SingletonBase<ResourceManager>, IInitialisable
    {
        [SerializeField] private ResourcePresenterFactory presenterFactory;
        
        public readonly HashSet<ResourceType> ExcludedTypes = new HashSet<ResourceType>
        {
            ResourceType.Coin,
            ResourceType.Lives
        };

        public ResourcePresenterFactory Factory => presenterFactory;
           
        protected override bool PersistAcrossScenes => true;
        
        public int InitializationPriority => 0; 
        public bool IsInitialized { get; private set; }
        private Dictionary<ResourceType, List<IResourceView>> registeredViews = new();

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Initialize the presenter factory
                if (presenterFactory == null)
                {
                    Debug.LogError("ResourcePresenterFactory reference is missing in ResourceManager!");
                    return UniTask.FromResult(false);
                }

                presenterFactory.Initialize();
                IsInitialized = true;
                return UniTask.FromResult(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize ResourceManager: {e.Message}");
                return UniTask.FromResult(false);
            }
        }
        public void RegisterView(ResourceType resourceType, IResourceView view)
        {
            if (!registeredViews.ContainsKey(resourceType))
            {
                registeredViews[resourceType] = new List<IResourceView>();
            }
        
            registeredViews[resourceType].Add(view);
        
            presenterFactory.CreatePresenter(resourceType, view);
        
            // Khởi tạo view nếu cần
            if (view is ResourceView resourceView)
            {
                var eventService = ServiceLocator.GetService<IEventService>();
                var timeManager = ServiceLocator.GetService<TimeManager>();
                resourceView.Init(resourceType, eventService, timeManager,presenterFactory);
            }
        }
    
        public void UnregisterView(ResourceType resourceType, IResourceView view)
        {
            if (registeredViews.TryGetValue(resourceType, out var views))
            {
                views.Remove(view);
            }
        }
#if UNITY_EDITOR
        [Button]
        void UpdateResources(ResourceType resourceType)
        {
            presenterFactory.ProcessPendingUpdates(resourceType);
        }
#endif
        
    }
}