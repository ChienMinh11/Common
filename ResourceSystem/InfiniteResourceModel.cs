using System;
using System.Collections.Generic;

namespace ChieChie.Core
{
    public class InfiniteResourceModel
    {
        private const string SAVE_KEY_PREFIX = "Resource_Inf_Time_";
        private readonly ISaveSystem _saveSystem;
        private readonly Dictionary<ResourceType, DateTime> _expirationTimes = new();

        public InfiniteResourceModel(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
        }

        public void Initialize()
        {
            var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
            foreach (var type in resourceTypes)
            {
                string key = SAVE_KEY_PREFIX + type.ToString();
                _saveSystem.RegisterKey(key);
                
                long savedTicks = _saveSystem.Load<long>(key, 0L);
                _expirationTimes[type] = savedTicks > 0 ? new DateTime(savedTicks, DateTimeKind.Utc) : DateTime.MinValue;
            }
        }

        public void AddDuration(ResourceType type, TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;

            DateTime now = DateTime.UtcNow;
            DateTime currentExpiration = _expirationTimes.TryGetValue(type, out var time) ? time : DateTime.MinValue;

            _expirationTimes[type] = (currentExpiration > now) ? currentExpiration.Add(duration) : now.Add(duration);
            _saveSystem.Save(SAVE_KEY_PREFIX + type.ToString(), _expirationTimes[type].Ticks);
        }

        public TimeSpan GetRemainingTime(ResourceType type)
        {
            if (!_expirationTimes.TryGetValue(type, out var expiration)) return TimeSpan.Zero;
            DateTime now = DateTime.UtcNow;
            return expiration <= now ? TimeSpan.Zero : expiration - now;
        }

        public bool IsInfinite(ResourceType type)
        {
            return GetRemainingTime(type) > TimeSpan.Zero;
        }
    }
}