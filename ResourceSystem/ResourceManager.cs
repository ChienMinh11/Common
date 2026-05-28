using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
    public class ResourceManager : MonoBehaviour, IResourceManager, IInitialisable, IReadOnlyInfiniteStatus
    {
        private const string RESOURCE_CONFIG_PATH = "Config/ResourceConfig";

        [SerializeField] private ResourceConfig resourceConfig;
        
        private ResourceModel<long> _longModel;
        private InfiniteResourceModel _infiniteModel;
        private IEventService _eventService;
        private ISaveSystem _saveSystem;

        private readonly List<IResourcePresenter> _activePresenters = new();

        public int InitializationPriority => 0;
        public bool IsInitialized { get; private set; }

        [Inject]
        private void Construct(IEventService eventService, ISaveSystem saveSystem)
        {
            _eventService = eventService;
            _saveSystem = saveSystem;
            // XÓA BỎ HOÀN TOÀN TRƯỜNG IResourcePolicy TẠI ĐÂY!
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (resourceConfig == null)
            {
                resourceConfig = Resources.Load<ResourceConfig>(RESOURCE_CONFIG_PATH);
            }

            if (_eventService == null)
            {
                Debug.LogError("[ResourceManager] Failed to get IEventService");
            }
        
            _longModel = new ResourceModel<long>(new LongConverter(), _eventService, _saveSystem, this);
            _longModel?.Initialize(resourceConfig);
            _infiniteModel = new InfiniteResourceModel(_saveSystem);
            _infiniteModel.Initialize();

            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        public IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view)
        {
            if (!IsInitialized) return null;

            var presenter = new ResourcePresenter<long>(
                _longModel, 
                view, 
                resourceType, 
                new LongConverter(), 
                _eventService, 
                this
            );

            if (presenter != null)
            {
               
                _activePresenters.Add(presenter);
            }

            return presenter;
        }

        public void UnregisterPresenter(IResourcePresenter presenter)
        {
            if (presenter == null) return;

            if (_activePresenters.Contains(presenter))
            {
                presenter.Cleanup();
                _activePresenters.Remove(presenter);
            }
        }

        #region Gameplay API

        [Button]
        public void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false)
        {
           _longModel?.AddResource(resourceType, amount, delayUpdate);
        }

        [Button]
        public bool SpendResource(ResourceType resourceType, long amount)
        {
            return _longModel?.SpendResource(resourceType, amount) ?? false;
          
        }

        [Button]
        public long GetCurrentAmount(ResourceType resourceType)
        {
            return _longModel != null ? _longModel.GetAmount(resourceType) : 0;
        }

        [Button]
        public bool IsAtMaxStack(ResourceType resourceType)
        {
            var resourceData = resourceConfig.GetResourceData(resourceType);
            if (resourceData == null || resourceData.MaxStack <= 0) return false;
            return GetCurrentAmount(resourceType) >= resourceData.MaxStack;
        }

        public void AddInfiniteDuration(ResourceType resourceType, TimeSpan duration)
        {
            if (!IsInitialized) return;
            _infiniteModel.AddDuration(resourceType, duration);
            ForceUpdateAllView(); // Render lại view ngay lập tức
        }

        public bool IsCurrentlyInfinite(ResourceType resourceType)
        {
            return IsInitialized && _infiniteModel.IsInfinite(resourceType);
        }

        public TimeSpan GetRemainingInfiniteTime(ResourceType resourceType)
        {
            return IsInitialized ? _infiniteModel.GetRemainingTime(resourceType) : TimeSpan.Zero;
        }
        
        TimeSpan IReadOnlyInfiniteStatus.GetRemainingInfiniteTime(ResourceType resourceType)
        {
            return this.GetRemainingInfiniteTime(resourceType);
        }

        public void ProcessPendingUpdate(ResourceType resourceType)
        {
            if (!IsInitialized) return;

            // Tìm presenter quản lý loại tài nguyên này và yêu cầu xử lý update tiếp theo
            var presenter = _activePresenters.Find(p => p.ResourceId == resourceType);
            presenter?.ProcessPendingUpdates();
        }

        [Button]
        public void ForceUpdateAllView()
        {
            if (!IsInitialized) return;

            for (int i = 0; i < _activePresenters.Count; i++)
            {
                _activePresenters[i].ForceUpdateView();
            }
        }

        #endregion

   

        private void OnDestroy()
        {
            foreach (var presenter in _activePresenters)
            {
                presenter?.Cleanup();
            }

            _activePresenters.Clear();
            _longModel?.Cleanup();
        }
    }
}