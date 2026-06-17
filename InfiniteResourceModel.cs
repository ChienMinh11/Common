using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Resource
{
    public class InfiniteResourceModel
    {
        private readonly IResourceSaveAdapter _saveAdapter;
        private ResourceConfig _config;
        private readonly Dictionary<int, DateTime> _expirationTimes = new();

        // --- CÁC ACTIONS THAY THẾ IEVENTSERVICE ---
        public event Action<int> OnInfiniteDurationAdded;
        public event Action<int> OnInfiniteDurationExpired;

        // Bỏ IEventService khỏi constructor
        public InfiniteResourceModel(IResourceSaveAdapter saveAdapter)
        {
            _saveAdapter = saveAdapter;
        }
     
        public void Initialize(ResourceConfig config)
        {
            _config = config;
            if (_config == null) return;

            foreach (var resourceData in _config.GetAllResources())
            {
                int hash = resourceData.HashId;
                _expirationTimes[hash] = _saveAdapter.LoadInfiniteExpiration(resourceData, DateTime.MinValue);
            }
        }

        public void AddDuration(int hash, TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;
            DateTime now = DateTime.UtcNow;
            DateTime currentExpiration = _expirationTimes.TryGetValue(hash, out var time) ? time : DateTime.MinValue;
            _expirationTimes[hash] = (currentExpiration > now) ? currentExpiration.Add(duration) : now.Add(duration);
         
            var resourceData = _config?.GetResourceData(hash);
            if (resourceData != null)
            {
                _saveAdapter.SaveInfiniteExpiration(resourceData, _expirationTimes[hash]);
            }
            
            // Invoke sự kiện Action
            OnInfiniteDurationAdded?.Invoke(hash);
        }

        public TimeSpan GetRemainingTime(int hash)
        {
            if (!_expirationTimes.TryGetValue(hash, out var expiration)) return TimeSpan.Zero;
            DateTime now = DateTime.UtcNow;

            if (expiration <= now)
            {
                if (expiration != DateTime.MinValue)
                {
                    _expirationTimes[hash] = DateTime.MinValue;
                    
                    var resourceData = _config?.GetResourceData(hash);
                    if (resourceData != null)
                    {
                        _saveAdapter.SaveInfiniteExpiration(resourceData, DateTime.MinValue);
                    }
                
                    // Invoke sự kiện Action khi hết hạn vô hạn
                    OnInfiniteDurationExpired?.Invoke(hash);
                }
                return TimeSpan.Zero;
            }
            return expiration - now;
        }

        public bool IsInfinite(int hash) => GetRemainingTime(hash) > TimeSpan.Zero;

        public void Cleanup()
        {
            OnInfiniteDurationAdded = null;
            OnInfiniteDurationExpired = null;
        }
    }
}