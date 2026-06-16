using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    public class InfiniteResourceModel
    {
        private readonly IResourceSaveAdapter _saveAdapter;
        private readonly IEventService _eventService;

        private ResourceConfig _config;
        
        private readonly Dictionary<int, DateTime> _expirationTimes = new();

        public InfiniteResourceModel(IResourceSaveAdapter saveAdapter, IEventService eventService)
        {
            _saveAdapter = saveAdapter;
            _eventService = eventService;
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
            
            _eventService?.Publish<int, SharedEventType>(SharedEventType.OnInfiniteDurationAdded, hash);
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
                
                    _eventService?.Publish<int, SharedEventType>(SharedEventType.OnInfiniteDurationExpired, hash);
                }
                return TimeSpan.Zero;
            }
            return expiration - now;
        }

        public bool IsInfinite(int hash) => GetRemainingTime(hash) > TimeSpan.Zero;
    }
  
}