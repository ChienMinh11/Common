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
        [SerializeField] private bool useLongNumbers = false;

        private ResourceModel<int> _intModel;
        private ResourceModel<long> _longModel;

        // Giữ bộ data lưu thời gian vô hạn hoàn toàn độc lập
        private InfiniteResourceModel _infiniteModel;

        private ResourcePresenterFactory _factory;
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

            _factory = new ResourcePresenterFactory(_eventService,this);

            // Khởi tạo Model quản lý thời gian
            _infiniteModel = new InfiniteResourceModel(_saveSystem);
            _infiniteModel.Initialize();

            // SỬA TẠI ĐÂY: Truyền trực tiếp "this" thay cho policy cũ
            if (useLongNumbers)
            {
                _longModel = new ResourceModel<long>(new LongConverter(), _eventService, _saveSystem, this);
                _longModel.Initialize(resourceConfig);
            }
            else
            {
                _intModel = new ResourceModel<int>(new IntConverter(), _eventService, _saveSystem, this);
                _intModel.Initialize(resourceConfig);
            }

            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        // Hiện thực từ IResourceManager
        public IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view)
        {
            if (!IsInitialized) return null;

            object activeModel = useLongNumbers ? (object)_longModel : (object)_intModel;
            var presenter = _factory.CreatePresenter(resourceType, view, activeModel, useLongNumbers);

            if (presenter != null)
            {
                _activePresenters.Add(presenter);
            }

            return presenter;
        }

        // Hiện thực từ IResourceManager
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
            if (useLongNumbers) _longModel?.AddResource(resourceType, amount, delayUpdate);
            else _intModel?.AddResource(resourceType, (int)amount, delayUpdate);
        }

        [Button]
        public bool SpendResource(ResourceType resourceType, long amount)
        {
            if (useLongNumbers) return _longModel?.SpendResource(resourceType, amount) ?? false;
            return _intModel?.SpendResource(resourceType, (int)amount) ?? false;
        }

        [Button]
        public long GetCurrentAmount(ResourceType resourceType)
        {
            if (useLongNumbers) return _longModel != null ? _longModel.GetAmount(resourceType) : 0;
            return _intModel != null ? _intModel.GetAmount(resourceType) : 0;
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
            _intModel?.Cleanup();
            _longModel?.Cleanup();
        }
    }
}