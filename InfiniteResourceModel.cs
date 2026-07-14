using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;

namespace ChieChie.Resource
{
    public class InfiniteResourceModel
    {
        private readonly IResourceSaveAdapter _saveAdapter;
        private ResourceConfig _config;
        private readonly Dictionary<string, DateTime> _expirationTimes = new();
        private readonly Dictionary<string, CancellationTokenSource> _expirationWatchers = new();

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
                    DateTime expirationTime = _saveAdapter.LoadInfiniteExpiration(resourceData, DateTime.MinValue);
                    if (expirationTime <= DateTime.UtcNow && expirationTime != DateTime.MinValue)
                    {
                        expirationTime = DateTime.MinValue;
                        _saveAdapter.SaveInfiniteExpiration(resourceData, DateTime.MinValue);
                    }
                    
                    _expirationTimes[id] = expirationTime;
                    ScheduleExpirationWatcher(id, expirationTime);
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
            ScheduleExpirationWatcher(resourceKey, _expirationTimes[resourceKey]);
            OnInfiniteDurationAdded?.Invoke(resourceKey, delayUpdate);
        }
        public TimeSpan GetRemainingTime(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey) || !_expirationTimes.TryGetValue(resourceKey, out var expiration)) 
                return TimeSpan.Zero;
                
            DateTime now = DateTime.UtcNow;

            if (expiration <= now)
            {
                ExpireIfDue(resourceKey, true);
                return TimeSpan.Zero;
            }
            return expiration - now;
        }

        public bool IsInfinite(string resourceKey) => GetRemainingTime(resourceKey) > TimeSpan.Zero;

        private void ScheduleExpirationWatcher(string resourceKey, DateTime expirationTime)
        {
            CancelExpirationWatcher(resourceKey);
            
            if (string.IsNullOrEmpty(resourceKey) || expirationTime == DateTime.MinValue)
            {
                return;
            }

            TimeSpan delay = expirationTime - DateTime.UtcNow;
            if (delay <= TimeSpan.Zero)
            {
                ExpireIfDue(resourceKey, false);
                return;
            }

            var cts = new CancellationTokenSource();
            _expirationWatchers[resourceKey] = cts;
            WatchExpirationAsync(resourceKey, delay, cts.Token).Forget();
        }

        private async UniTaskVoid WatchExpirationAsync(string resourceKey, TimeSpan delay, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(delay, delayType: DelayType.Realtime, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!token.IsCancellationRequested)
            {
                ExpireIfDue(resourceKey, false);
            }
        }

        private void ExpireIfDue(string resourceKey, bool cancelWatcher)
        {
            if (string.IsNullOrEmpty(resourceKey) || !_expirationTimes.TryGetValue(resourceKey, out var expiration))
            {
                return;
            }

            if (expiration == DateTime.MinValue || expiration > DateTime.UtcNow)
            {
                return;
            }

            _expirationTimes[resourceKey] = DateTime.MinValue;
            
            var resourceData = _config?.GetResourceData(resourceKey);
            if (resourceData != null)
            {
                _saveAdapter.SaveInfiniteExpiration(resourceData, DateTime.MinValue);
            }

            if (cancelWatcher)
            {
                CancelExpirationWatcher(resourceKey);
            }
            else if (_expirationWatchers.TryGetValue(resourceKey, out var cts))
            {
                _expirationWatchers.Remove(resourceKey);
                cts.Dispose();
            }
            
            OnInfiniteDurationExpired?.Invoke(resourceKey);
        }

        private void CancelExpirationWatcher(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey) || !_expirationWatchers.TryGetValue(resourceKey, out var cts))
            {
                return;
            }

            _expirationWatchers.Remove(resourceKey);
            cts.Cancel();
            cts.Dispose();
        }

        public void Cleanup()
        {
            foreach (var cts in _expirationWatchers.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            
            _expirationWatchers.Clear();
            _expirationTimes.Clear();
            OnInfiniteDurationAdded = null;
            OnInfiniteDurationExpired = null;
        }
    }
}
