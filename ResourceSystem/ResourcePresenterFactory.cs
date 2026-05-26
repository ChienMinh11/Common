using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework
{
    public class ResourcePresenterFactory : MonoBehaviour
    {
        [SerializeField] private ResourceConfig resourceConfig;
        [SerializeField] private bool useLongNumbers = false;
        
        private ResourceModel<int> intModel;
        private ResourceModel<long> longModel;
        private IntConverter intConverter;
        private LongConverter longConverter;
        private IEventService eventService;
        private TimeManager timeManager;
       // private Dictionary<ResourceType, object> presenters;
        
        private Dictionary<ResourceType, List<object>> presentersByType; // Track nhiều presenter cùng type
        private Dictionary<object, ResourceType> presenters; // Map ngược từ presenter về type


        public ResourceModel<int> IntModel => intModel;
        public ResourceModel<long> LongModel => longModel;
        public bool UseLongNumbes => useLongNumbers;
        
        public ResourceConfig ResourceConfig => resourceConfig;

        public void Initialize()
        {
            
            eventService = ServiceLocator.GetService<IEventService>();
            timeManager = ServiceLocator.GetService<TimeManager>();
            if (eventService == null)
            {
                Debug.LogError("[ResourcePresenterFactory] Failed to get IEventService from ServiceLocator");
                return;
            }

            intConverter = new IntConverter();
            longConverter = new LongConverter();
            
            EnsureModelInitialized();
        
            presentersByType = new Dictionary<ResourceType, List<object>>();
            presenters = new Dictionary<object, ResourceType>();
            InitializeDefaultResources();
        }
        private void EnsureModelInitialized()
        {
            if (useLongNumbers && longModel == null)
            {
                longModel = new ResourceModel<long>(longConverter, eventService);
                longModel.Initialize(resourceConfig);
            }
            else if (!useLongNumbers && intModel == null)
            {
                intModel = new ResourceModel<int>(intConverter, eventService);
                intModel.Initialize(resourceConfig);
            }
        }
        public void InitializeDefaultResources()
        {
            EnsureModelInitialized();
    
            if (useLongNumbers)
            {
                longModel?.InitializeDefaultValues();
            }
            else
            {
                intModel?.InitializeDefaultValues();
            }
        }

        public ResourcePresenter<int> CreateIntPresenter(ResourceType resourceId, IResourceView view)
        {
            if (intModel == null)
            {
                Debug.LogError("[ResourcePresenterFactory] Int model not initialized. Make sure useLongNumbers is false.");
                return null;
            }

            var presenter = new ResourcePresenter<int>(
                intModel, 
                view, 
                resourceId, 
                intConverter,
                eventService,
                timeManager
            );
   
            // Thêm vào list thay vì ghi đè
            if (!presentersByType.ContainsKey(resourceId))
            {
                presentersByType[resourceId] = new List<object>();
            }
            presentersByType[resourceId].Add(presenter);
            presenters[presenter] = resourceId;
   
            return presenter;
        }

        public ResourcePresenter<long> CreateLongPresenter(ResourceType resourceId, IResourceView view)
        {
            if (longModel == null)
            {
                Debug.LogError("[ResourcePresenterFactory] Long model not initialized. Make sure useLongNumbers is true.");
                return null;
            }

            var presenter = new ResourcePresenter<long>(
                longModel, 
                view, 
                resourceId, 
                longConverter,
                eventService,
                timeManager
            );
   
            // Thêm vào list thay vì ghi đè  
            if (!presentersByType.ContainsKey(resourceId))
            {
                presentersByType[resourceId] = new List<object>();
            }
            presentersByType[resourceId].Add(presenter);
            presenters[presenter] = resourceId;
   
            return presenter;
        }

        public object CreatePresenter(ResourceType resourceId, IResourceView view)
        {
            var presenter = useLongNumbers 
                ? CreateLongPresenter(resourceId, view) 
                : CreateIntPresenter(resourceId, view) as object;
            if (!presentersByType.ContainsKey(resourceId))
            {
                presentersByType[resourceId] = new List<object>();
            }
            presentersByType[resourceId].Add(presenter);
            presenters[presenter] = resourceId;
    
            return presenter;
        }
        [Button]
        public void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false)
        {
           EnsureModelInitialized();
            if (useLongNumbers)
            {
                longModel?.AddResource(resourceType, amount, delayUpdate);
            }
            else
            {
                intModel?.AddResource(resourceType, (int)amount, delayUpdate);
            }
           
        }
        [Button]
        public bool SpendResource(ResourceType resourceType, long amount)
        {
            bool success = false;
            if (useLongNumbers)
            {
                success = longModel?.SpendResource(resourceType, amount) ?? false;
            }
            else
            {
                success = intModel?.SpendResource(resourceType, (int)amount) ?? false;
            }
            return success;
        }

        public void ProcessPendingUpdates(ResourceType resourceType)
        {
    
            if (presentersByType.TryGetValue(resourceType, out var presenterList))
            {
                for (int i = presenterList.Count - 1; i >= 0; i--)
                {
                    var presenter = presenterList[i];
                    if (presenter == null)
                    {
                        presenterList.RemoveAt(i);
                        continue;
                    }
            
                    if (presenter is ResourcePresenter<long> longPresenter)
                    {
                        longPresenter.ProcessPendingUpdates();
                    }
                    else if (presenter is ResourcePresenter<int> intPresenter)
                    {
                        intPresenter.ProcessPendingUpdates();
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No presenters found for {resourceType}");
            }
        }
        public void ForceUpdateAllResources()
        {
            foreach (var presenterList in presentersByType.Values)
            {
                foreach (var presenter in presenterList)
                {
                    if (presenter is ResourcePresenter<long> longPresenter)
                    {
                        longPresenter.ForceUpdateView();
                    }
                    else if (presenter is ResourcePresenter<int> intPresenter)
                    {
                        intPresenter.ForceUpdateView();
                    }
                }
            }
        }
        [Button]
        public void AddInfiniteResource(ResourceType resourceType, float duration, bool delayUpdate = false)
        {
            if (useLongNumbers)
            {
                longModel?.SetInfiniteResource(resourceType, duration, delayUpdate);
            }
            else
            {
                intModel?.SetInfiniteResource(resourceType, duration, delayUpdate);
            }
        }

        public void RemoveInfiniteResource(ResourceType resourceType)
        {
            if (useLongNumbers)
            {
                longModel?.RemoveInfiniteResource(resourceType);
            }
            else
            {
                intModel?.RemoveInfiniteResource(resourceType);
            }
        }

        public bool IsInfiniteResource(ResourceType resourceType)
        {
            if (useLongNumbers)
            {
                return longModel?.IsInfiniteResource(resourceType) ?? false;
            }
            else
            {
                return intModel?.IsInfiniteResource(resourceType) ?? false;
            }
        }
        public bool SetMaxStack(ResourceType resourceType, long newMaxStack)
        {
            bool success = false;
   
            if (presentersByType.TryGetValue(resourceType, out var presenterList))
            {
                foreach (var presenter in presenterList)
                {
                    if (presenter is ResourcePresenter<long> longPresenter)
                    {
                        if (longPresenter.SetMaxStack(newMaxStack))
                        {
                            success = true;
                        }
                    }
                    else if (presenter is ResourcePresenter<int> intPresenter)
                    {
                        if (intPresenter.SetMaxStack(newMaxStack))
                        {
                            success = true;
                        }
                    }
                }
            }
   
            return success;
        }
        [Button("Reset All Infinite Resources")]
        public void ResetAllInfiniteResources()
        {
            if (useLongNumbers)
            {
                longModel?.ResetAllInfiniteResources();
            }
            else
            {
                intModel?.ResetAllInfiniteResources();
            }
    
            Debug.Log("[ResourcePresenterFactory] All infinite resources have been reset.");
            //ForceUpdateAllResources();
        }
        [Button]
        public long GetCurrentAmount(ResourceType resourceType)
        {
            if (useLongNumbers)
            {
                if (longModel != null)
                {
                    return longModel.GetAmount(resourceType);
                }
            }
            else
            {
                if (intModel != null)
                {
                    return intModel.GetAmount(resourceType);
                }
            }
            return 0;
        }

        // Optional helper method to check if resource is at max stack
        public bool IsAtMaxStack(ResourceType resourceType)
        {
            var resourceData = resourceConfig.GetResourceData(resourceType);
            if (resourceData == null || resourceData.MaxStack <= 0)
                return false;

            var currentAmount = GetCurrentAmount(resourceType);
            return currentAmount >= resourceData.MaxStack;
        }
        public void RemovePresenter(object presenter)
        {
            if (presenters.TryGetValue(presenter, out var resourceType))
            {
                if (presentersByType.TryGetValue(resourceType, out var list))
                {
                    list.Remove(presenter);
                    if (list.Count == 0)
                    {
                        presentersByType.Remove(resourceType);
                    }
                }
                presenters.Remove(presenter);
            }
        }
        public long GetMaxStack(ResourceType resourceType)
        {
            var resourceData = resourceConfig.GetResourceData(resourceType);
            return resourceData?.MaxStack ?? 0;
        }


        private void OnDestroy()
        {
            // Cleanup presenters
            foreach (var presenterList in presentersByType.Values)
            {
                foreach (var presenter in presenterList)
                {
                    if (presenter is ResourcePresenter<int> intPresenter)
                    {
                        intPresenter.Cleanup();
                    }
                    else if (presenter is ResourcePresenter<long> longPresenter)
                    {
                        longPresenter.Cleanup();
                    }
                }
            }
            presentersByType.Clear();
            presenters.Clear();

            // Cleanup models
            if (intModel != null)
            {
                intModel.Cleanup();
                intModel = null;
            }

            if (longModel != null)
            {
                longModel.Cleanup();
                longModel = null;
            }
        }
    }
}