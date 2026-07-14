using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Resource;
using Cysharp.Threading.Tasks;
using Game.GamePlay;
using VContainer;
using VContainer.Unity;

namespace Game.DependencyInjection
{
    public sealed class ResourceServiceInstaller : IInstaller
    {
        private readonly ResourceConfig _config;
        private readonly ResourceLifecycleBridge _resourceLifecycleBridge;

        public ResourceServiceInstaller(ResourceConfig config, ResourceLifecycleBridge _bridge)
        {
            _config = config;
            _resourceLifecycleBridge = _bridge;
        }

        public void Install(IContainerBuilder builder)
        {
            if (_config != null) builder.RegisterInstance(_config);
   
            builder.Register<ResourceSaveAdapter>(Lifetime.Singleton).As<IResourceSaveAdapter>();

            builder.Register<ResourceModel>(Lifetime.Singleton)
                .As<IDisposable>()
                .AsSelf();
        

            builder.Register<ResourcePresenterFactory>(Lifetime.Singleton)
                .As<IPresenterFactory<IResourcePresenter, IResourceView>>();
         
            builder.Register<ResourceService>(Lifetime.Singleton)
                            .As<IResourceService>()
                            .AsSelf();
            
            builder.RegisterComponent(_resourceLifecycleBridge);
        }
    }
    public sealed class ResourcePresenterFactory
        : IPresenterFactory<IResourcePresenter, IResourceView>
    {
        private readonly ResourceModel _model;

        public ResourcePresenterFactory(ResourceModel model)
        {
            _model = model;
        }

        public IResourcePresenter Create(IResourceView view)
        {
            return new ResourcePresenter(view, _model);
        }
    }
      public sealed class ResourceSaveAdapter : IResourceSaveAdapter
    {
        private const string RESOURCE_SAVE_KEY = "Resource_SaveData_Key";
        private readonly ISaveSystem _saveSystem;
        
        private ResourceSaveData _currentRuntimeData;
        private readonly Dictionary<string, ResourceSaveEntry> _entryCache = new();

        public ResourceSaveAdapter(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
            
            // Đăng ký 1 Key duy nhất cho toàn bộ hệ thống tài nguyên
            _saveSystem.RegisterKey<ResourceSaveData>(
                RESOURCE_SAVE_KEY,
                () => _currentRuntimeData,
                isAutoSave: false
            );

            LoadRootData();
        }

        private void LoadRootData()
        {
            _currentRuntimeData = _saveSystem.Load<ResourceSaveData>(RESOURCE_SAVE_KEY, null);
            if (_currentRuntimeData == null)
            {
                _currentRuntimeData = new ResourceSaveData();
            }
            _entryCache.Clear();
            foreach (var entry in _currentRuntimeData.resources)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.resourceId))
                {
                    _entryCache[entry.resourceId] = entry;
                }
            }
        }

        private ResourceSaveEntry GetOrCreateEntry(string resId)
        {
            if (_entryCache.TryGetValue(resId, out var entry))
            {
                return entry;
            }

            entry = new ResourceSaveEntry { resourceId = resId };
            _currentRuntimeData.resources.Add(entry);
            _entryCache[resId] = entry;
            return entry;
        }

        private void SaveRootData()
        {
            _saveSystem.Save(RESOURCE_SAVE_KEY, _currentRuntimeData);
        }

        public void RegisterResource(IResourceData resourceData)
        {
            GetOrCreateEntry(resourceData.ResourceId);
        }

        #region Amount

        public void SaveAmount(IResourceData resourceData, long amount)
        {
            var entry = GetOrCreateEntry(resourceData.ResourceId);
            entry.amount = amount;
            entry.hasSavedAmount = true;
            SaveRootData();
        }

        public long LoadAmount(IResourceData resourceData, long fallbackValue)
        {
            if (_entryCache.TryGetValue(resourceData.ResourceId, out var entry) && entry.hasSavedAmount)
            {
                return entry.amount;
            }
            return fallbackValue;
        }

        #endregion

        #region Infinite Duration

        public void SaveInfiniteExpiration(IResourceData resourceData, DateTime expirationTime)
        {
            var entry = GetOrCreateEntry(resourceData.ResourceId);
            entry.infiniteExpirationTicks = expirationTime.Ticks;
            entry.hasSavedInfinite = true;
            SaveRootData();
        }

        public DateTime LoadInfiniteExpiration(IResourceData resourceData, DateTime fallbackValue)
        {
            if (_entryCache.TryGetValue(resourceData.ResourceId, out var entry) && entry.hasSavedInfinite)
            {
                long ticks = entry.infiniteExpirationTicks;
                return ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
            }
            return DateTime.MinValue;
        }

        #endregion

        #region Regen Status

        public void SaveRegenStatus(IResourceData resourceData, bool isEnabled)
        {
            var entry = GetOrCreateEntry(resourceData.ResourceId);
            entry.isRegenEnabled = isEnabled;
            entry.hasSavedRegenStatus = true;
            SaveRootData();
        }

        public bool LoadRegenStatus(IResourceData resourceData, bool defaultValue)
        {
            if (_entryCache.TryGetValue(resourceData.ResourceId, out var entry) && entry.hasSavedRegenStatus)
            {
                return entry.isRegenEnabled;
            }
            return defaultValue;
        }

        #endregion

        #region Regen Time

        public void SaveNextRegenTime(IResourceData resourceData, DateTime nextRegenTime)
        {
            var entry = GetOrCreateEntry(resourceData.ResourceId);
            entry.nextRegenTimeTicks = nextRegenTime.Ticks;
            entry.hasSavedRegenTime = true;
            SaveRootData();
        }

        public DateTime LoadNextRegenTime(IResourceData resourceData, DateTime fallbackValue)
        {
            if (_entryCache.TryGetValue(resourceData.ResourceId, out var entry) && entry.hasSavedRegenTime)
            {
                long ticks = entry.nextRegenTimeTicks;
                return ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : fallbackValue;
            }
            return fallbackValue;
        }

        #endregion

        #region First Init

        public bool IsFirstInit() => !_currentRuntimeData.isFirstInitComplete;
        
        public void SetFirstInitComplete()
        {
            _currentRuntimeData.isFirstInitComplete = true;
            SaveRootData();
        }

        #endregion
    }
      public sealed class ResourceService : IResourceService
      {
          private readonly ResourceModel _model;

          public ResourceService(ResourceModel model)
          {
              _model = model;
          }
          public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
          {
              _model.Initialize();
              return UniTask.FromResult(true);
          }

          public void AddResource(string resourceKey, long amount, bool delayUpdate = false)
          {
              _model.AddResource(resourceKey,amount,delayUpdate);
          }

          public bool SpendResource(string resourceKey, long amount)
          {
              return _model.SpendResource(resourceKey, amount);
          }

          public long GetCurrentAmount(string resourceKey)
          {
             return _model.GetCurrentAmount(resourceKey);
          }

          public bool IsAtMaxStack(string resourceKey)
          {
              return   _model.IsAtMaxStack(resourceKey);
          }

          public long GetMaxStack(string resourceKey)
          {
              return _model.GetMaxStack(resourceKey);
          }

          public void SetMaxStackAndFill(string resourceKey, long newMaxStack, bool fillFull = false)
          {
              _model.SetMaxStackAndFill(resourceKey, newMaxStack, fillFull);
          }

          public void ProcessPendingUpdate(string resourceKey, long amountIncrement = 0)
          {
              _model.ProcessPendingUpdate(resourceKey, amountIncrement);
          }

          public void ForceUpdateAllView()
          {
              _model.ForceUpdateAllView();
          }

          public void AddInfiniteDuration(string resourceKey, TimeSpan duration, bool delayUpdate = false)
          {
              _model.AddInfiniteDuration(resourceKey, duration, delayUpdate);
          }

          public bool IsCurrentlyInfinite(string resourceKey)
          {
             return _model.IsCurrentlyInfinite(resourceKey);
          }

          public TimeSpan GetRemainingInfiniteTime(string resourceKey)
          {
              return _model.GetRemainingInfiniteTime(resourceKey);
          }

          public bool IsRegenEnabled(string resourceKey)
          {
              return _model.IsRegenEnabled(resourceKey);
          }

          public DateTime GetNextRegenTime(string resourceKey)
          {
              return _model.GetNextRegenTime(resourceKey);
          }

          public void SetRegenStatus(string resourceKey, bool isEnabled)
          {
              _model.SetRegenStatus(resourceKey, isEnabled);
          }

          public event Action<string> OnInfiniteExpired
          {
              add => _model.OnInfiniteExpired += value;
              remove => _model.OnInfiniteExpired -= value;
          }
          public event Action<string, bool> OnInfiniteAdded
          {
              add => _model.OnInfiniteAdded += value;
              remove => _model.OnInfiniteAdded -= value;
          }
          public void OnAppQuit()
          {
              _model.OnAppQuit();
          }

          public void OnAppPause(bool pauseStatus)
          {
              _model.OnAppPause(pauseStatus);
          }
      }
}
