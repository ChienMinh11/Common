using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    public class InfiniteResourceModel
    {
        private const string SAVE_KEY_PREFIX = "Resource_Inf_Time_";
        private readonly ISaveSystem _saveSystem;
        private readonly IEventService _eventService;

        private ResourceConfig _config;
        
        private readonly Dictionary<int, DateTime> _expirationTimes = new();

        public InfiniteResourceModel(ISaveSystem saveSystem, IEventService eventService)
        {
            _saveSystem = saveSystem;
            _eventService = eventService;
        }
     
        public void Initialize(ResourceConfig config)
        {
            _config = config;

            if (_config == null)  return;

            foreach (var resourceData in _config.GetAllResources())
            {
                int hash = resourceData.HashId;
                string key = SAVE_KEY_PREFIX + resourceData.ResourceId;
                _saveSystem.RegisterKey(key);
                
                long savedTicks = _saveSystem.Load<long>(key, 0L);
                _expirationTimes[hash] = savedTicks > 0 ? new DateTime(savedTicks, DateTimeKind.Utc) : DateTime.MinValue;
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
                string key = SAVE_KEY_PREFIX + resourceData.ResourceId;
                _saveSystem.Save(key, _expirationTimes[hash].Ticks);
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
                        string key = SAVE_KEY_PREFIX + resourceData.ResourceId;
                        _saveSystem.Save(key, DateTime.MinValue.Ticks);
                    }
                
                    _eventService?.Publish<int, SharedEventType>(SharedEventType.OnInfiniteDurationExpired, hash);
                    Debug.Log($"Infinite Duration Expired: {expiration}");
                }
                return TimeSpan.Zero;
            }
            
            return expiration - now;
        }

        public bool IsInfinite(int hash)
        {
            return GetRemainingTime(hash) > TimeSpan.Zero;
        }
    }
}