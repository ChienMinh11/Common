using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Resource;

namespace Game.GamePlay
{
    public class ResourceSaveAdapter : IResourceSaveAdapter
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

            // Đồng bộ dữ liệu từ List vào Dictionary Cache để truy xuất O(1)
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
            // Chỉ khởi tạo cấu trúc sẵn trong RAM, không cần đăng ký nhiều key lẻ với SaveSystem nữa
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
}