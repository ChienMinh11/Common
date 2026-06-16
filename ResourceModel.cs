using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    public enum ResourceEventType
    {
        ResourceChanged,
        ResourceSpent,
        ResourceMaxStackReached,
        ResourceAdded,
        ResourceInsufficient,
    }

    public class ResourceModel<T>
    {
        private const string SAVE_KEY_PREFIX = "Resource_";
        private const string FIRST_INIT_KEY = "ResourceFirstInit";

        private ResourceConfig _config;
        private IResourceService _resourceService;

        private readonly INumberConverter<T> _converter;
        private readonly IEventService _eventService;
        private readonly ISaveSystem _saveSystem;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;

        private readonly Dictionary<int, T> _resourceAmounts = new();
        private readonly Dictionary<int, string> _cachedSaveKeys = new();

        public ResourceModel(INumberConverter<T> converter, IEventService eventService, ISaveSystem saveSystem,
            IReadOnlyInfiniteStatus infiniteStatus)
        {
            this._converter = converter;
            this._eventService = eventService;
            this._saveSystem = saveSystem;
            this._infiniteStatus = infiniteStatus;
        }

        public void Initialize(ResourceConfig config)
        {
            _config = config;

            _saveSystem.RegisterKey(FIRST_INIT_KEY);
         
            foreach (var resourceData in config.GetAllResources())
            {
                _cachedSaveKeys[resourceData.HashId] = SAVE_KEY_PREFIX + resourceData.ResourceId;
                var saveKey = GetSaveKey(resourceData.HashId, resourceData.ResourceId);
                _saveSystem.RegisterKey(saveKey);
            }

            bool isFirstInit = !_saveSystem.Load<bool>(FIRST_INIT_KEY, false);

            if (isFirstInit)
            {
                // UnityEngine.Debug.Log(
                //     "[ResourceModel] Khởi tạo lần đầu! Nạp cấu hình mặc định (MaxStack / DefaultAmount) từ Config.");

                foreach (var resourceData in config.GetAllResources())
                {
                    long initialAmount = 0;

                    if (resourceData.HasRegen)
                    {
                        initialAmount = resourceData.MaxStack;
                    }
                    else if (resourceData.DefaultAmount > 0)
                    {
                        initialAmount = resourceData.DefaultAmount;
                    }

                    var convertedAmt = _converter.FromLong(initialAmount);
                    _resourceAmounts[resourceData.HashId] = convertedAmt;

                    SaveAmount(resourceData.HashId, resourceData.ResourceId, convertedAmt);
                }

                _saveSystem.Save(FIRST_INIT_KEY, true);
            }
            else
            {
                // UnityEngine.Debug.Log(
                //     "[ResourceModel] Trạng thái game cũ phát hiện. Tiến hành nạp dữ liệu từ File Save.");

                foreach (var resourceData in config.GetAllResources())
                {
                    long fallbackValue = resourceData.HasRegen ? resourceData.MaxStack : resourceData.DefaultAmount;

                    _resourceAmounts[resourceData.HashId] = LoadAmountWithFallback(resourceData.HashId,
                        resourceData.ResourceId, fallbackValue);
                }
            }
        }

        private T LoadAmountWithFallback(int hash, string resourceId, long fallbackValue)
        {
            var saveKey = GetSaveKey(hash, resourceId);

            long savedLong = _saveSystem.Load<long>(saveKey, fallbackValue);

            return _converter.FromLong(savedLong);
        }

        private string GetSaveKey(int hash, string resourceId)
        {
            if (_cachedSaveKeys.TryGetValue(hash, out var key)) return key;
            string newKey = SAVE_KEY_PREFIX + resourceId;
            _cachedSaveKeys[hash] = newKey;
            return newKey;
        }

        private T LoadAmount(int hash, string resourceId)
        {
            var saveKey = GetSaveKey(hash, resourceId);
            long savedLong = _saveSystem.Load<long>(saveKey, 0L);
            return _converter.FromLong(savedLong);
        }

        private void SaveAmount(int hash, string resourceId, T amount)
        {
            var saveKey = GetSaveKey(hash, resourceId);
            long longAmount = _converter.ToLong(amount);
            _saveSystem.Save<long>(saveKey, longAmount);
        }

        public T GetAmount(string resourceId)
        {
            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);
            return _resourceAmounts.TryGetValue(hash, out var amount) ? amount : _converter.Zero;
        }

        public void AddResource(string resourceId, T amount, bool delayUpdate = false)
        {
            if (_converter.IsLessThan(amount, _converter.Zero)) return;

            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);
            var currentAmount = _resourceAmounts.TryGetValue(hash, out var amt) ? amt : _converter.Zero;
            var newAmount = _converter.Add(currentAmount, amount);

            var maxStack = GetMaxStack(hash);
            if (maxStack > 0)
            {
                long longNewAmount = _converter.ToLong(newAmount);
                if (longNewAmount > maxStack)
                {
                    newAmount = _converter.FromLong(maxStack);
                    _eventService.Publish(ResourceEventType.ResourceMaxStackReached, this, hash);
                }
            }

            _resourceAmounts[hash] = newAmount;
            SaveAmount(hash, resourceId, newAmount);

            var changeData = new ResourceChangeData<T>(hash, currentAmount, newAmount, delayUpdate);
            _eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
            _eventService.Publish(ResourceEventType.ResourceAdded, this, changeData);
        }

        public bool SpendResource(string resourceId, T amount)
        {
            if (_converter.IsLessThan(amount, _converter.Zero)) return false;

            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);

            if (_infiniteStatus != null && _infiniteStatus.IsCurrentlyInfinite(hash))
            {
                var currentAmt = _resourceAmounts.TryGetValue(hash, out var amt) ? amt : _converter.Zero;
                var changeData = new ResourceChangeData<T>(hash, currentAmt, currentAmt);
                _eventService.Publish(ResourceEventType.ResourceSpent, this, changeData);
                return true;
            }

            var currentAmount = _resourceAmounts.TryGetValue(hash, out var cAmt) ? cAmt : _converter.Zero;
            if (_converter.IsLessThan(currentAmount, amount))
            {
                _eventService.Publish(ResourceEventType.ResourceInsufficient, this, hash);
                return false;
            }

            var newAmount = _converter.Subtract(currentAmount, amount);
            _resourceAmounts[hash] = newAmount;
            SaveAmount(hash, resourceId, newAmount);

            var changeDataNormal = new ResourceChangeData<T>(hash, currentAmount, newAmount);
            _eventService.Publish(ResourceEventType.ResourceChanged, this, changeDataNormal);
            _eventService.Publish(ResourceEventType.ResourceSpent, this, changeDataNormal);
            return true;
        }

        public ResourceData GetResourceData(int hash) => _config.GetResourceData(hash);
        public long GetMaxStack(int hash) => _config.GetResourceData(hash)?.MaxStack ?? 0;

        public bool SetMaxStack(int hash, long newMaxStack)
        {
            var resourceData = _config.GetResourceData(hash);
            if (resourceData == null) return false;

            resourceData.MaxStack = newMaxStack;
            if (newMaxStack > 0)
            {
                var currentAmount = _resourceAmounts.TryGetValue(hash, out var amt) ? amt : _converter.Zero;
                long longCurrentAmount = _converter.ToLong(currentAmount);
                if (longCurrentAmount > newMaxStack)
                {
                    var newAmount = _converter.FromLong(newMaxStack);
                    _resourceAmounts[hash] = newAmount;
                    SaveAmount(hash, resourceData.ResourceId, newAmount);

                    var changeData = new ResourceChangeData<T>(hash, currentAmount, newAmount);
                    _eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
                }
            }

            return true;
        }

        public void Cleanup()
        {
            _resourceAmounts.Clear();
            _cachedSaveKeys.Clear();
        }
    }
}