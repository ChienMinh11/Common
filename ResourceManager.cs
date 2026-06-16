using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine; // Cần thêm để sử dụng Animator.StringToHash

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

        private IDisposable _eventSubscription;
        private readonly List<IResourcePresenter> _activePresenters = new();
        public bool IsInitialized { get; private set; }

        public ResourceManager(ResourceConfig resourceConfig, IEventService eventService, ISaveSystem saveSystem)
        {
            _resourceConfig = resourceConfig;
            _eventService = eventService;
            _saveSystem = saveSystem;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _longModel = new ResourceModel<long>(new LongConverter(), _eventService, _saveSystem, this);
            _longModel?.Initialize(_resourceConfig);
            _infiniteModel = new InfiniteResourceModel(_saveSystem, _eventService);
            _infiniteModel.Initialize(_resourceConfig);

            _resourceRegenController = new ResourceRegenController();
            _resourceRegenController.Initialize(this, _saveSystem);
            IsInitialized = true;
            return UniTask.FromResult(true);
        }
      

        public IResourcePresenter RegisterView(string resourceKey, IResourceView view)
        {
            if (!IsInitialized) return null;

            var presenter = new ResourcePresenter<long>(_longModel, view, resourceKey, new LongConverter(), _eventService, this);
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

        public void AddResource(string resourceKey, long amount, bool delayUpdate = false) => _longModel?.AddResource(resourceKey, amount, delayUpdate);
        public bool SpendResource(string resourceKey, long amount) => _longModel?.SpendResource(resourceKey, amount) ?? false;
        public long GetCurrentAmount(string resourceKey) => _longModel != null ? _longModel.GetAmount(resourceKey) : 0;
        
        public bool IsAtMaxStack(string resourceKey)
        {
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            var resourceData = _resourceConfig.GetResourceData(hash);
            if (resourceData == null || resourceData.MaxStack <= 0) return false;
            return GetCurrentAmount(resourceKey) >= resourceData.MaxStack;
        }

        public long GetMaxStack(string resourceKey)
        {
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            return _resourceConfig?.GetResourceData(hash)?.MaxStack ?? 0;
        }

        public void SetMaxStackAndFill(string resourceKey, long newMaxStack, bool fillFull = false)
        {
            if (!IsInitialized) return;
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _longModel?.SetMaxStack(hash, newMaxStack);
            if (fillFull)
            {
                long currentAmount = GetCurrentAmount(resourceKey);
                if (currentAmount < newMaxStack) AddResource(resourceKey, newMaxStack - currentAmount);
            }
        }

        public void AddInfiniteDuration(string resourceKey, TimeSpan duration)
        {
            if (!IsInitialized) return;
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _infiniteModel.AddDuration(hash, duration);
            ForceUpdateAllView();
        }

        public bool IsCurrentlyInfinite(string resourceKey)
        {
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            return IsCurrentlyInfinite(hash);
        }

        public TimeSpan GetRemainingInfiniteTime(string resourceKey)
        {
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            return IsInitialized ? _infiniteModel.GetRemainingTime(hash) : TimeSpan.Zero;
        }
        public bool IsCurrentlyInfinite(int resourceHash) => IsInitialized && _infiniteModel.IsInfinite(resourceHash);
        
        TimeSpan IReadOnlyInfiniteStatus.GetRemainingInfiniteTime(int resourceHash) 
            => IsInitialized ? _infiniteModel.GetRemainingTime(resourceHash) : TimeSpan.Zero;

        public ResourceConfig GetConfig() => _resourceConfig;
  
        public bool IsRegenEnabled(string resourceKey)
        {
            if (_resourceRegenController == null) return false;
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            return _resourceRegenController.IsRegenEnabled(hash);
        }

        public DateTime GetNextRegenTime(string resourceKey)
        {
            if (_resourceRegenController == null) return DateTime.UtcNow;
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            return _resourceRegenController.GetNextRegenTime(hash);
        }

        public void SetRegenStatus(string resourceKey, bool isEnabled)
        {
            if (_resourceRegenController == null) return;
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _resourceRegenController.SetRegenStatus(hash, isEnabled);
        }

        public void ProcessPendingUpdate(string resourceKey)
        {
            if (!IsInitialized) return;
            int hash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _activePresenters.Find(p => p.ResourceHash == hash)?.ProcessPendingUpdates();
        }

        public void ForceUpdateAllView()
        {
            if (!IsInitialized) return;
            for (int i = 0; i < _activePresenters.Count; i++) _activePresenters[i].ForceUpdateView();
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