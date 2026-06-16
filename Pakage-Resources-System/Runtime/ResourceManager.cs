using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using R3;

namespace ChieChie.Resource
{
    public class ResourceManager : IServiceInitialisable, IResourceService, IReadOnlyInfiniteStatus, IDisposable
    {
        
        private readonly ResourceConfig _resourceConfig;
        private ResourceRegenController _resourceRegenController;
        
        private ResourceModel<long> _longModel;
        private InfiniteResourceModel _infiniteModel;
        private readonly IEventService _eventService;
        private readonly ISaveSystem _saveSystem;
        private readonly IIconProvider _iconProvider;
        private IDisposable _eventSubscription;

        private readonly List<IResourcePresenter> _activePresenters = new();
        public bool IsInitialized { get; private set; }

        public ResourceManager(ResourceConfig resourceConfig,IEventService eventService, ISaveSystem saveSystem, IIconProvider iconProvider)
        {
            _resourceConfig = resourceConfig;
            _eventService = eventService;
            _saveSystem = saveSystem;
            _iconProvider = iconProvider;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _longModel = new ResourceModel<long>(new LongConverter(), _eventService, _saveSystem, this);
            _longModel?.Initialize(_resourceConfig);
            _infiniteModel = new InfiniteResourceModel(_saveSystem, _eventService);
            _infiniteModel.Initialize();

            _resourceRegenController = new ResourceRegenController();
            _resourceRegenController.Initialize(this, _saveSystem);
            RegEvent();
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        private void RegEvent()
        {
            if (_eventService != null)
            {
                _eventSubscription = _eventService
                    .Observe<List<ResourceRewardCommand>, SharedEventType>(SharedEventType.RequestAddResource)
                    .Subscribe(OnResourceRewardRequested);
            }
        }

        public IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view)
        {
            if (!IsInitialized) return null;

            var presenter = new ResourcePresenter<long>(_longModel, view, resourceType, new LongConverter(), _eventService, this, _iconProvider);
            if (presenter != null) _activePresenters.Add(presenter);
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

        public void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false) => _longModel?.AddResource(resourceType, amount, delayUpdate);
        public bool SpendResource(ResourceType resourceType, long amount) => _longModel?.SpendResource(resourceType, amount) ?? false;
        public long GetCurrentAmount(ResourceType resourceType) => _longModel != null ? _longModel.GetAmount(resourceType) : 0;
        
        public bool IsAtMaxStack(ResourceType resourceType)
        {
            var resourceData = _resourceConfig.GetResourceData(resourceType);
            if (resourceData == null || resourceData.MaxStack <= 0) return false;
            return GetCurrentAmount(resourceType) >= resourceData.MaxStack;
        }

        public long GetMaxStack(ResourceType resourceType) => _resourceConfig?.GetResourceData(resourceType)?.MaxStack ?? 0;

        public void SetMaxStackAndFill(ResourceType resourceType, long newMaxStack, bool fillFull = false)
        {
            if (!IsInitialized) return;
            _longModel?.SetMaxStack(resourceType, newMaxStack);
            if (fillFull)
            {
                long currentAmount = GetCurrentAmount(resourceType);
                if (currentAmount < newMaxStack) AddResource(resourceType, newMaxStack - currentAmount);
            }
        }

        public void AddInfiniteDuration(ResourceType resourceType, TimeSpan duration)
        {
            if (!IsInitialized) return;
            _infiniteModel.AddDuration(resourceType, duration);
            ForceUpdateAllView();
        }

        public bool IsCurrentlyInfinite(ResourceType resourceType) => IsInitialized && _infiniteModel.IsInfinite(resourceType);
        public TimeSpan GetRemainingInfiniteTime(ResourceType resourceType) => IsInitialized ? _infiniteModel.GetRemainingTime(resourceType) : TimeSpan.Zero;
        public ResourceConfig GetConfig() => _resourceConfig;
        public bool IsRegenEnabled(ResourceType type) => _resourceRegenController != null && _resourceRegenController.IsRegenEnabled(type);
        public DateTime GetNextRegenTime(ResourceType type) => _resourceRegenController != null ? _resourceRegenController.GetNextRegenTime(type) : DateTime.UtcNow;
        public void SetRegenStatus(ResourceType type, bool isEnabled) => _resourceRegenController?.SetRegenStatus(type, isEnabled);

        TimeSpan IReadOnlyInfiniteStatus.GetRemainingInfiniteTime(ResourceType resourceType) => GetRemainingInfiniteTime(resourceType);

        public void ProcessPendingUpdate(ResourceType resourceType)
        {
            if (!IsInitialized) return;
            _activePresenters.Find(p => p.ResourceId == resourceType)?.ProcessPendingUpdates();
        }

        public void ForceUpdateAllView()
        {
            if (!IsInitialized) return;
            for (int i = 0; i < _activePresenters.Count; i++) _activePresenters[i].ForceUpdateView();
        }
        
        private void OnResourceRewardRequested(List<ResourceRewardCommand> commands)
        {
            if (commands == null || !IsInitialized) return;
            foreach (var cmd in commands)
            {
                if (cmd.IsInfinite) AddInfiniteDuration(cmd.ResourceType, TimeSpan.FromSeconds(cmd.DurationInSeconds));
                else AddResource(cmd.ResourceType, cmd.Amount);
            }
        }

        public void OnAppQuit() => _resourceRegenController?.SaveAllRegenTimes();
    
        public void OnAppPause(bool pauseStatus) 
        { 
            if (pauseStatus) _resourceRegenController?.SaveAllRegenTimes(); 
        }

        public void Dispose()
        {
            _eventSubscription?.Dispose();
            foreach (var presenter in _activePresenters) presenter?.Cleanup();
            _activePresenters.Clear();
            _longModel?.Cleanup();
            _resourceRegenController?.Dispose();
        }
    }
}