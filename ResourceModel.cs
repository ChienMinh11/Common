using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourceModel<T>
    {
        private ResourceConfig _config;

        private readonly INumberConverter<T> _converter;
        private readonly IResourceSaveAdapter _saveAdapter;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;

        private readonly Dictionary<string, T> _resourceAmounts = new();

        public event Action<ResourceChangeData<T>> OnResourceChanged;
        public event Action<ResourceChangeData<T>> OnResourceSpent;
        public event Action<ResourceChangeData<T>> OnResourceAdded;
        public event Action<string> OnResourceMaxStackReached;
        public event Action<string> OnResourceInsufficient;

        public ResourceModel(INumberConverter<T> converter, IResourceSaveAdapter saveAdapter,
            IReadOnlyInfiniteStatus infiniteStatus)
        {
            this._converter = converter;
            this._saveAdapter = saveAdapter;
            this._infiniteStatus = infiniteStatus;
        }

        public void Initialize(ResourceConfig config)
        {
            _config = config;

            foreach (var resourceData in config.GetAllResources())
            {
                _saveAdapter.RegisterResource(resourceData);
            }

            if (_saveAdapter.IsFirstInit())
            {
                Debug.Log($"[ResourceModel] khởi tạo lần đầu! Nạp cấu hình mặc định từ Config");
                foreach (var resourceData in config.GetAllResources())
                {
                    long initialAmount = resourceData.HasRegen ? resourceData.MaxStack : resourceData.DefaultAmount;

                    var convertedAmt = _converter.FromLong(initialAmount);
                    _resourceAmounts[resourceData.ResourceId] = convertedAmt;

                    _saveAdapter.SaveAmount(resourceData, initialAmount);
                }
                _saveAdapter.SetFirstInitComplete(); 
            }
            else
            {  
                Debug.Log($"[ResourceModel] Trạng thái game cũ phát hiện. Tiến hành nạp dữ liệu từ file save");
                foreach (var resourceData in config.GetAllResources())
                {
                    long fallbackValue = resourceData.HasRegen ? resourceData.MaxStack : resourceData.DefaultAmount;
                    long savedLong = _saveAdapter.LoadAmount(resourceData, fallbackValue);
                    
                    _resourceAmounts[resourceData.ResourceId] = _converter.FromLong(savedLong);
                }
            }
        }

        private void SaveAmount(ResourceData resourceData, T amount)
        {
            long longAmount = _converter.ToLong(amount);
            _saveAdapter.SaveAmount(resourceData, longAmount);
        }

        public T GetAmount(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey)) return _converter.Zero;
            return _resourceAmounts.TryGetValue(resourceKey, out var amount) ? amount : _converter.Zero;
        }

        public void AddResource(string resourceKey, T amount, bool delayUpdate = false)
        {
            if (string.IsNullOrEmpty(resourceKey) || _converter.IsLessThan(amount, _converter.Zero)) return;

            var resourceData = GetResourceData(resourceKey);
            if (resourceData == null) return;

            var currentAmount = _resourceAmounts.TryGetValue(resourceKey, out var amt) ? amt : _converter.Zero;
            var newAmount = _converter.Add(currentAmount, amount);

            var maxStack = GetMaxStack(resourceKey);
            if (maxStack > 0)
            {
                long longNewAmount = _converter.ToLong(newAmount);
                if (longNewAmount > maxStack)
                {
                    newAmount = _converter.FromLong(maxStack);
                    OnResourceMaxStackReached?.Invoke(resourceKey);
                }
            }

            _resourceAmounts[resourceKey] = newAmount;
            SaveAmount(resourceData, newAmount);

            var changeData = new ResourceChangeData<T>(resourceKey, currentAmount, newAmount, delayUpdate);
            
            OnResourceChanged?.Invoke(changeData);
            OnResourceAdded?.Invoke(changeData);
        }

        public bool SpendResource(string resourceKey, T amount)
        {
            if (string.IsNullOrEmpty(resourceKey) || _converter.IsLessThan(amount, _converter.Zero)) return false;

            var resourceData = GetResourceData(resourceKey);
            if (resourceData == null) return false;

            if (_infiniteStatus != null && _infiniteStatus.IsCurrentlyInfinite(resourceKey))
            {
                var currentAmt = _resourceAmounts.TryGetValue(resourceKey, out var amt) ? amt : _converter.Zero;
                var changeData = new ResourceChangeData<T>(resourceKey, currentAmt, currentAmt);
                OnResourceSpent?.Invoke(changeData);
                return true;
            }

            var currentAmount = _resourceAmounts.TryGetValue(resourceKey, out var cAmt) ? cAmt : _converter.Zero;
            if (_converter.IsLessThan(currentAmount, amount))
            {
                OnResourceInsufficient?.Invoke(resourceKey);
                return false;
            }

            var newAmount = _converter.Subtract(currentAmount, amount);
            _resourceAmounts[resourceKey] = newAmount;
            SaveAmount(resourceData, newAmount);

            var changeDataNormal = new ResourceChangeData<T>(resourceKey, currentAmount, newAmount);
            OnResourceChanged?.Invoke(changeDataNormal);
            OnResourceSpent?.Invoke(changeDataNormal);
            return true;
        }

        public ResourceData GetResourceData(string resourceKey) => _config.GetResourceData(resourceKey);
        public long GetMaxStack(string resourceKey) => _config.GetResourceData(resourceKey)?.MaxStack ?? 0;

        public bool SetMaxStack(string resourceKey, long newMaxStack)
        {
            var resourceData = _config.GetResourceData(resourceKey);
            if (resourceData == null) return false;

            resourceData.MaxStack = newMaxStack;
            if (newMaxStack > 0)
            {
                var currentAmount = _resourceAmounts.TryGetValue(resourceKey, out var amt) ? amt : _converter.Zero;
                long longCurrentAmount = _converter.ToLong(currentAmount);
                if (longCurrentAmount > newMaxStack)
                {
                    var newAmount = _converter.FromLong(newMaxStack);
                    _resourceAmounts[resourceKey] = newAmount;
                    SaveAmount(resourceData, newAmount);

                    var changeData = new ResourceChangeData<T>(resourceKey, currentAmount, newAmount);
                    OnResourceChanged?.Invoke(changeData);
                }
            }

            return true;
        }

        public void Cleanup()
        {
            _resourceAmounts.Clear();
            OnResourceChanged = null;
            OnResourceSpent = null;
            OnResourceAdded = null;
            OnResourceMaxStackReached = null;
            OnResourceInsufficient = null;
        }
    }
}