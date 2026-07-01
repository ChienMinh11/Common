using System;
using System.Collections.Generic;
using ChieChie.Constracts;

namespace ChieChie.Resource
{
    public class InfiniteResourceModel
    {
        private readonly IResourceSaveAdapter _saveAdapter;
        private ResourceConfig _config;
        private readonly Dictionary<string, DateTime> _expirationTimes = new();

        public event Action<string, bool> OnInfiniteDurationAdded;
        public event Action<string> OnInfiniteDurationExpired;

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
                string id = resourceData.ResourceId;
                if (!string.IsNullOrEmpty(id))
                {
                    _expirationTimes[id] = _saveAdapter.LoadInfiniteExpiration(resourceData, DateTime.MinValue);
                }
            }
        }

        public void AddDuration(string resourceKey, TimeSpan duration, bool delayUpdate = false)
        {
            if (string.IsNullOrEmpty(resourceKey) || duration <= TimeSpan.Zero) return;
    
            DateTime now = DateTime.UtcNow;
            DateTime currentExpiration = _expirationTimes.TryGetValue(resourceKey, out var time) ? time : DateTime.MinValue;
            _expirationTimes[resourceKey] = (currentExpiration > now) ? currentExpiration.Add(duration) : now.Add(duration);
 
            var resourceData = _config?.GetResourceData(resourceKey);
            if (resourceData != null)
            {
                _saveAdapter.SaveInfiniteExpiration(resourceData, _expirationTimes[resourceKey]);
            }
    
            // Truyền thêm flag delayUpdate vào Event công bố
            OnInfiniteDurationAdded?.Invoke(resourceKey, delayUpdate);
        }
        public TimeSpan GetRemainingTime(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey) || !_expirationTimes.TryGetValue(resourceKey, out var expiration)) 
                return TimeSpan.Zero;
                
            DateTime now = DateTime.UtcNow;

            if (expiration <= now)
            {
                if (expiration != DateTime.MinValue)
                {
                    _expirationTimes[resourceKey] = DateTime.MinValue;
                    
                    var resourceData = _config?.GetResourceData(resourceKey);
                    if (resourceData != null)
                    {
                        _saveAdapter.SaveInfiniteExpiration(resourceData, DateTime.MinValue);
                    }
                
                    OnInfiniteDurationExpired?.Invoke(resourceKey);
                }
                return TimeSpan.Zero;
            }
            return expiration - now;
        }

        public bool IsInfinite(string resourceKey) => GetRemainingTime(resourceKey) > TimeSpan.Zero;

        public void Cleanup()
        {
            _expirationTimes.Clear();
            OnInfiniteDurationAdded = null;
            OnInfiniteDurationExpired = null;
        }
    }
}