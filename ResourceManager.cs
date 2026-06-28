using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourceManager : IResourceService, IDisposable
    {
        private readonly ResourceConfig _resourceConfig;
        private ResourceRegenController _resourceRegenController;
        
        private ResourceModel<long> _longModel;
        private InfiniteResourceModel _infiniteModel;
        private readonly IResourceSaveAdapter _saveAdapter;

        private readonly List<IResourcePresenter> _activePresenters = new();
        private readonly Dictionary<IResourceView, ResourceRegenPresenter> _activeRegenPresenters = new();
        public bool IsInitialized { get; private set; }
        
        public event Action<string> OnInfiniteExpired;
        public event Action<string, bool> OnInfiniteAdded;

        public ResourceManager(ResourceConfig resourceConfig, IResourceSaveAdapter saveAdapter)
        {
            _resourceConfig = resourceConfig;
            _saveAdapter = saveAdapter;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _longModel = new ResourceModel<long>(new LongConverter(), _saveAdapter, this);
            _longModel.Initialize(_resourceConfig);
    
            _infiniteModel = new InfiniteResourceModel(_saveAdapter);
            _infiniteModel.Initialize(_resourceConfig);
            _infiniteModel.OnInfiniteDurationAdded += HandleInfiniteDurationAdded;
            _infiniteModel.OnInfiniteDurationExpired += HandleInfiniteDurationExpired;

            _resourceRegenController = new ResourceRegenController();
            _resourceRegenController.Initialize(this, _saveAdapter);
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        public IResourcePresenter RegisterView(string resourceKey, IResourceView view)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[{nameof(ResourceManager)}] Đang đăng ký View khi chưa Init xong Key: {resourceKey}");
                return null;
            }
            
            var presenter = new ResourcePresenter<long>(
                _longModel,
                view,
                resourceKey,
                new LongConverter(),
                this
            );

            if (presenter != null)
            {
                _activePresenters.Add(presenter);
              
                if (view is IResourceView regenView)
                {
                    var regenPresenter = new ResourceRegenPresenter(
                        regenView,
                        resourceKey,
                        this  
                    );
            
                    regenPresenter.Initialize(); 
                    _activeRegenPresenters[view] = regenPresenter; 
                }
            }

            return presenter;
        }

        public void UnregisterPresenter(IResourcePresenter presenter)
        {
            if (presenter == null) return;
    
            _activePresenters.Remove(presenter);

            if (presenter is ResourcePresenter<long> longPresenter && longPresenter.View != null)
            {
                if (_activeRegenPresenters.TryGetValue(longPresenter.View, out var regenPresenter))
                {
                    regenPresenter.Dispose();
                    _activeRegenPresenters.Remove(longPresenter.View);
                }
            }
        }

        public void AddResource(string resourceKey, long amount, bool delayUpdate = false) => _longModel?.AddResource(resourceKey, amount, delayUpdate);
        public bool SpendResource(string resourceKey, long amount) => _longModel?.SpendResource(resourceKey, amount) ?? false;
        public long GetCurrentAmount(string resourceKey) => _longModel != null ? _longModel.GetAmount(resourceKey) : 0;
        
        public bool IsAtMaxStack(string resourceKey)
        {
            var resourceData = _resourceConfig.GetResourceData(resourceKey);
            if (resourceData == null || resourceData.MaxStack <= 0) return false;
            return GetCurrentAmount(resourceKey) >= resourceData.MaxStack;
        }

        public long GetMaxStack(string resourceKey)
        {
            return _resourceConfig?.GetResourceData(resourceKey)?.MaxStack ?? 0;
        }

        public void SetMaxStackAndFill(string resourceKey, long newMaxStack, bool fillFull = false)
        {
            if (!IsInitialized) return;
            _longModel?.SetMaxStack(resourceKey, newMaxStack);
            if (fillFull)
            {
                long currentAmount = GetCurrentAmount(resourceKey);
                if (currentAmount < newMaxStack) AddResource(resourceKey, newMaxStack - currentAmount);
            }
        }

        public void AddInfiniteDuration(string resourceKey, TimeSpan duration, bool delayUpdate = false)
        {
            if (!IsInitialized) return;
            _infiniteModel.AddDuration(resourceKey, duration, delayUpdate);
            if (!delayUpdate)
            {
                ForceUpdateAllView();
            }
        }

        public bool IsCurrentlyInfinite(string resourceKey)
        {
            return IsInitialized && _infiniteModel.IsInfinite(resourceKey);
        }

        public TimeSpan GetRemainingInfiniteTime(string resourceKey)
        {
            return IsInitialized ? _infiniteModel.GetRemainingTime(resourceKey) : TimeSpan.Zero;
        }

        public ResourceConfig GetConfig() => _resourceConfig;
        public ResourceModel<long> LongModel => _longModel;
        public InfiniteResourceModel InfiniteModel => _infiniteModel;
  
        public bool IsRegenEnabled(string resourceKey)
        {
            if (_resourceRegenController == null) return false;
            return _resourceRegenController.IsRegenEnabled(resourceKey);
        }

        public DateTime GetNextRegenTime(string resourceKey)
        {
            if (_resourceRegenController == null) return DateTime.UtcNow;
            return _resourceRegenController.GetNextRegenTime(resourceKey);
        }

        public void SetRegenStatus(string resourceKey, bool isEnabled)
        {
            if (_resourceRegenController == null) return;
            _resourceRegenController.SetRegenStatus(resourceKey, isEnabled);
        }

        public void ProcessPendingUpdate(string resourceKey)
        {
            if (!IsInitialized) return;
            _activePresenters.Find(p => p.ResourceKey == resourceKey)?.ProcessPendingUpdates();
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
        private void HandleInfiniteDurationAdded(string resourceKey, bool delayUpdate) => OnInfiniteAdded?.Invoke(resourceKey, delayUpdate);
        private void HandleInfiniteDurationExpired(string resourceKey) => OnInfiniteExpired?.Invoke(resourceKey);

        public void Dispose()
        {
            if (_infiniteModel != null)
            {
                _infiniteModel.OnInfiniteDurationAdded -= HandleInfiniteDurationAdded;
                _infiniteModel.OnInfiniteDurationExpired -= HandleInfiniteDurationExpired;
            }
            foreach (var presenter in _activePresenters) presenter?.Cleanup();
            _activePresenters.Clear();
            _longModel?.Cleanup();
            _infiniteModel?.Cleanup();
            _resourceRegenController?.Dispose();
        }
    }
}