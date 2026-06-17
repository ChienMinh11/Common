using System;
using System.Collections.Generic;
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
        private ResourceConfig _config;
        private IResourceService _resourceService;

        private readonly INumberConverter<T> _converter;
        private readonly IResourceSaveAdapter _saveAdapter;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;

        private readonly Dictionary<int, T> _resourceAmounts = new();
        private readonly Dictionary<int, string> _cachedSaveKeys = new();

        public event Action<ResourceChangeData<T>> OnResourceChanged;
        public event Action<ResourceChangeData<T>> OnResourceSpent;
        public event Action<ResourceChangeData<T>> OnResourceAdded;
        public event Action<int> OnResourceMaxStackReached;
        public event Action<int> OnResourceInsufficient;

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
                UnityEngine.Debug.Log($"[ResourceModel] khởi tạo lần đầu! Nạp cấu hình mặc định (Max Stack / Defaut Amount) từ Config");
                foreach (var resourceData in config.GetAllResources())
                {
                    long initialAmount = resourceData.HasRegen ? resourceData.MaxStack : resourceData.DefaultAmount;

                    var convertedAmt = _converter.FromLong(initialAmount);
                    _resourceAmounts[resourceData.HashId] = convertedAmt;

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
                    
                    _resourceAmounts[resourceData.HashId] = _converter.FromLong(savedLong);
                }
            }
        }

        private void SaveAmount(ResourceData resourceData, T amount)
        {
            long longAmount = _converter.ToLong(amount);
            _saveAdapter.SaveAmount(resourceData, longAmount);
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
            
            var resourceData = GetResourceData(hash);
            if (resourceData == null) return;

            var currentAmount = _resourceAmounts.TryGetValue(hash, out var amt) ? amt : _converter.Zero;
            var newAmount = _converter.Add(currentAmount, amount);

            var maxStack = GetMaxStack(hash);
            if (maxStack > 0)
            {
                long longNewAmount = _converter.ToLong(newAmount);
                if (longNewAmount > maxStack)
                {
                    newAmount = _converter.FromLong(maxStack);
                    // Invoke Action thay vì Publish
                    OnResourceMaxStackReached?.Invoke(hash);
                }
            }

            _resourceAmounts[hash] = newAmount;
            SaveAmount(resourceData, newAmount);

            var changeData = new ResourceChangeData<T>(hash, currentAmount, newAmount, delayUpdate);
            
            // Invoke các Action sự kiện tương ứng
            OnResourceChanged?.Invoke(changeData);
            OnResourceAdded?.Invoke(changeData);
        }

        public bool SpendResource(string resourceId, T amount)
        {
            if (_converter.IsLessThan(amount, _converter.Zero)) return false;

            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);
            
            var resourceData = GetResourceData(hash);
            if (resourceData == null) return false;

            if (_infiniteStatus != null && _infiniteStatus.IsCurrentlyInfinite(hash))
            {
                var currentAmt = _resourceAmounts.TryGetValue(hash, out var amt) ? amt : _converter.Zero;
                var changeData = new ResourceChangeData<T>(hash, currentAmt, currentAmt);
                OnResourceSpent?.Invoke(changeData);
                return true;
            }

            var currentAmount = _resourceAmounts.TryGetValue(hash, out var cAmt) ? cAmt : _converter.Zero;
            if (_converter.IsLessThan(currentAmount, amount))
            {
                OnResourceInsufficient?.Invoke(hash);
                return false;
            }

            var newAmount = _converter.Subtract(currentAmount, amount);
            _resourceAmounts[hash] = newAmount;
            SaveAmount(resourceData, newAmount);

            var changeDataNormal = new ResourceChangeData<T>(hash, currentAmount, newAmount);
            OnResourceChanged?.Invoke(changeDataNormal);
            OnResourceSpent?.Invoke(changeDataNormal);
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
                    SaveAmount(resourceData, newAmount);

                    var changeData = new ResourceChangeData<T>(hash, currentAmount, newAmount);
                    OnResourceChanged?.Invoke(changeData);
                }
            }

            return true;
        }

        public void Cleanup()
        {
            _resourceAmounts.Clear();
            _cachedSaveKeys.Clear();
            // Clear sự kiện tránh Memory Leak
            OnResourceChanged = null;
            OnResourceSpent = null;
            OnResourceAdded = null;
            OnResourceMaxStackReached = null;
            OnResourceInsufficient = null;
        }
    }
}