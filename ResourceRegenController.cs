using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourceRegenController : IDisposable
    {
        private const string REGEN_STATUS_KEY_PREFIX = "Resource_Regen_Enabled_";
        private const string REGEN_NEXT_TIME_KEY_PREFIX = "Resource_Regen_NextTime_";

        private IResourceService _resourceService;
        private IResourceSaveAdapter _saveAdapter;
        private CancellationTokenSource _cts;
        private ResourceConfig _resourceConfig;

        private readonly Dictionary<int, bool> _activeStatuses = new();
        private readonly Dictionary<int, DateTime> _nextRegenTimes = new();

        public void Initialize(IResourceService resourceService, IResourceSaveAdapter saveAdapter)
        {
            _resourceService = resourceService;
            _saveAdapter = saveAdapter;
            _resourceConfig = _resourceService.GetConfig();
    
            if (_resourceConfig == null) return;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                int hash = setting.HashId;
        
                bool isEnabled = _saveAdapter.LoadRegenStatus(setting, setting.IsEnabledByDefault);
                _activeStatuses[hash] = isEnabled;
        
                _nextRegenTimes[hash] = _saveAdapter.LoadNextRegenTime(setting, DateTime.UtcNow.AddSeconds(setting.IntervalSeconds));
            }
  
            ProcessOfflineRegen();
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            StartRegenCheckLoopAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid StartRegenCheckLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                DateTime now = DateTime.UtcNow;

                foreach (var setting in _resourceConfig.GetAllRegenSettings())
                {
                    int hash = setting.HashId;
                    string resKey = setting.ResourceId;

                    if (_activeStatuses.TryGetValue(hash, out bool isEnabled) && isEnabled)
                    {
                        if (_resourceService.IsAtMaxStack(resKey))
                        {
                            _nextRegenTimes[hash] = now.AddSeconds(setting.IntervalSeconds);
                            continue;
                        }

                        if (now >= _nextRegenTimes[hash])
                        {
                            _resourceService.AddResource(resKey, setting.RegenAmount);
                            _nextRegenTimes[hash] = _nextRegenTimes[hash].AddSeconds(setting.IntervalSeconds);
                            _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[hash]);
                        }
                    }
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(1), delayTiming: PlayerLoopTiming.Update, cancellationToken: token);
            }
        }

        private void ProcessOfflineRegen()
        {
            DateTime now = DateTime.UtcNow;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                int hash = setting.HashId; 
                string resKey = setting.ResourceId;

                if (_activeStatuses.TryGetValue(hash, out bool isEnabled) && isEnabled)
                {
                    if (_resourceService.IsAtMaxStack(resKey)) continue;

                    DateTime nextTime = _nextRegenTimes[hash];
                    if (now >= nextTime)
                    {
                        TimeSpan overdue = now - nextTime;
                        int extraCycles = (int)(overdue.TotalSeconds / setting.IntervalSeconds) + 1;

                        long totalRegenAmount = extraCycles * setting.RegenAmount;
                        _resourceService.AddResource(resKey, totalRegenAmount);
                        _nextRegenTimes[hash] = nextTime.AddSeconds(extraCycles * setting.IntervalSeconds);
                        _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[hash]);
                    }
                }
            }
        }

        #region PublicAPI

        // SỬA: Chuyển đổi tham số định danh các hàm Public API từ ResourceType sang int (Hash)
        public void SetRegenStatus(int hash, bool isEnabled)
        {
            if (_activeStatuses.ContainsKey(hash))
            {
                _activeStatuses[hash] = isEnabled;
                
                var setting = _resourceConfig.GetResourceData(hash);
                if (setting != null)
                {
                    _saveAdapter.SaveRegenStatus(setting, isEnabled);
                    
                    if (isEnabled && setting.HasRegen)
                    {
                        _nextRegenTimes[hash] = DateTime.UtcNow.AddSeconds(setting.IntervalSeconds);
                        _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[hash]);
                    }
                }
            }
        }

        public bool IsRegenEnabled(int hash)
        {
            return _activeStatuses.TryGetValue(hash, out bool isEnabled) && isEnabled;
        }

        public void SetRegenAmount(int hash, long newAmount)
        {
            var data = _resourceConfig.GetResourceData(hash);
            if (data != null && data.HasRegen)
            {
                data.RegenAmount = newAmount;
            }
        }

        public DateTime GetNextRegenTime(int hash)
        {
            return _nextRegenTimes.TryGetValue(hash, out var time) ? time : DateTime.UtcNow;
        }

        public void SaveAllRegenTimes()
        {
            if (_resourceConfig == null || _saveAdapter == null) return;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                int hash = setting.HashId;
              
                if (_activeStatuses.TryGetValue(hash, out bool isEnabled) && isEnabled)
                {
                    if (_nextRegenTimes.TryGetValue(hash, out DateTime nextTime))
                    {
                        _saveAdapter.SaveNextRegenTime(setting, _nextRegenTimes[hash]);
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
        }
    }
}