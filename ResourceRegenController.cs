using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;

namespace ChieChie.Resource
{
    public class ResourceRegenController : IDisposable
    {
        private ResourceModel _resourceModel;
        private IResourceSaveAdapter _saveAdapter;
        private CancellationTokenSource _cts;
        private ResourceConfig _resourceConfig;

        private readonly Dictionary<string, bool> _activeStatuses = new();
        private readonly Dictionary<string, DateTime> _nextRegenTimes = new();

        public void Initialize(
            ResourceModel resourceModel,
            ResourceConfig resourceConfig,
            IResourceSaveAdapter saveAdapter)
        {
            _resourceModel = resourceModel;
            _saveAdapter = saveAdapter;
            _resourceConfig = resourceConfig;
    
            if (_resourceConfig == null) return;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                string id = setting.ResourceId;
                if (string.IsNullOrEmpty(id)) continue;
        
                bool isEnabled = _saveAdapter.LoadRegenStatus(setting, setting.IsEnabledByDefault);
                _activeStatuses[id] = isEnabled;
        
                _nextRegenTimes[id] = _saveAdapter.LoadNextRegenTime(setting, DateTime.UtcNow.AddSeconds(setting.IntervalSeconds));
            }
  
            ProcessOfflineRegen();
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            StartRegenCheckLoopAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid StartRegenCheckLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    DateTime now = DateTime.UtcNow;

                    foreach (var setting in _resourceConfig.GetAllRegenSettings())
                    {
                        string resKey = setting.ResourceId;
                        if (string.IsNullOrEmpty(resKey)) continue;

                        if (_activeStatuses.TryGetValue(resKey, out bool isEnabled) && isEnabled)
                        {
                            if (_resourceModel.IsAtMaxStack(resKey))
                            {
                                _nextRegenTimes[resKey] = now.AddSeconds(setting.IntervalSeconds);
                                continue;
                            }

                            if (now >= _nextRegenTimes[resKey])
                            {
                                _nextRegenTimes[resKey] = _nextRegenTimes[resKey]
                                    .AddSeconds(setting.IntervalSeconds);
                                _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[resKey]);
                                _resourceModel.AddResource(resKey, setting.RegenAmount);
                            }
                        }
                    }

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(1),
                        delayTiming: PlayerLoopTiming.Update,
                        cancellationToken: token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Expected when the model is disposed.
            }
        }

        private void ProcessOfflineRegen()
        {
            DateTime now = DateTime.UtcNow;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                string resKey = setting.ResourceId;
                if (string.IsNullOrEmpty(resKey)) continue;

                if (_activeStatuses.TryGetValue(resKey, out bool isEnabled) && isEnabled)
                {
                    if (_resourceModel.IsAtMaxStack(resKey)) continue;

                    DateTime nextTime = _nextRegenTimes[resKey];
                    if (now >= nextTime)
                    {
                        TimeSpan overdue = now - nextTime;
                        int extraCycles = (int)(overdue.TotalSeconds / setting.IntervalSeconds) + 1;

                        long totalRegenAmount = extraCycles * setting.RegenAmount;
                        _resourceModel.AddResource(resKey, totalRegenAmount);
                        _nextRegenTimes[resKey] = nextTime.AddSeconds(extraCycles * setting.IntervalSeconds);
                        _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[resKey]);
                    }
                }
            }
        }

        #region PublicAPI

        public void SetRegenStatus(string resourceKey, bool isEnabled)
        {
            if (string.IsNullOrEmpty(resourceKey)) return;

            _activeStatuses[resourceKey] = isEnabled;
            
            var setting = _resourceConfig.GetResourceData(resourceKey);
            if (setting != null)
            {
                _saveAdapter.SaveRegenStatus(setting, isEnabled);
                
                if (isEnabled && setting.HasRegen)
                {
                    _nextRegenTimes[resourceKey] = DateTime.UtcNow.AddSeconds(setting.IntervalSeconds);
                    _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[resourceKey]);
                }
            }
        }

        public bool IsRegenEnabled(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey)) return false;
            return _activeStatuses.TryGetValue(resourceKey, out bool isEnabled) && isEnabled;
        }

        public void SetRegenAmount(string resourceKey, long newAmount)
        {
            var data = _resourceConfig.GetResourceData(resourceKey);
            if (data != null && data.HasRegen)
            {
                data.RegenAmount = newAmount;
            }
        }

        public DateTime GetNextRegenTime(string resourceKey)
        {
            if (string.IsNullOrEmpty(resourceKey)) return DateTime.UtcNow;
            return _nextRegenTimes.TryGetValue(resourceKey, out var time) ? time : DateTime.UtcNow;
        }

        public void SaveAllRegenTimes()
        {
            if (_resourceConfig == null || _saveAdapter == null) return;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                string resKey = setting.ResourceId;
                if (string.IsNullOrEmpty(resKey)) continue;
              
                if (_activeStatuses.TryGetValue(resKey, out bool isEnabled) && isEnabled)
                {
                    if (_nextRegenTimes.TryGetValue(resKey, out DateTime nextTime))
                    {
                        _saveAdapter.SaveNextRegenTime(setting, nextTime);
                    }
                }
            }
        }

        #endregion

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _activeStatuses.Clear();
            _nextRegenTimes.Clear();
        }
    }
}
