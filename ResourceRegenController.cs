using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;

namespace ChieChie.Resource
{
    public class ResourceRegenController : IDisposable
    {
        private const string REGEN_STATUS_KEY_PREFIX = "Resource_Regen_Enabled_";
        private const string REGEN_NEXT_TIME_KEY_PREFIX = "Resource_Regen_NextTime_";

        private IResourceService _resourceService;
        private ISaveSystem _saveSystem;
        private CancellationTokenSource _cts;
        private ResourceConfig _resourceConfig;

        private readonly Dictionary<ResourceType, bool> _activeStatuses = new();
        private readonly Dictionary<ResourceType, DateTime> _nextRegenTimes = new();

        public void Initialize(IResourceService resourceService, ISaveSystem saveSystem)
        {
            _resourceService = resourceService;
            _saveSystem = saveSystem;
            _resourceConfig = _resourceService.GetConfig();
            
            if (_resourceConfig == null) return;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                ResourceType type = setting.key;
                
                string statusKey = REGEN_STATUS_KEY_PREFIX + type;
                _saveSystem.RegisterKey(statusKey);
                
                bool isEnabled = _saveSystem.Load(statusKey, setting.IsEnabledByDefault);
                _activeStatuses[type] = isEnabled;
                
                string timeKey = REGEN_NEXT_TIME_KEY_PREFIX + type;
                _saveSystem.RegisterKey(timeKey);
                
                long savedTicks = _saveSystem.Load<long>(timeKey, 0L);
                if (savedTicks > 0)
                {
                    _nextRegenTimes[type] = new DateTime(savedTicks, DateTimeKind.Utc);
                }
                else
                {
                    _nextRegenTimes[type] = DateTime.UtcNow.AddSeconds(setting.IntervalSeconds);
                }
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
                    ResourceType type = setting.key;

                    if (_activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled)
                    {
                        if (_resourceService.IsAtMaxStack(type))
                        {
                            _nextRegenTimes[type] = now.AddSeconds(setting.IntervalSeconds);
                            continue;
                        }

                        if (now >= _nextRegenTimes[type])
                        {
                            _resourceService.AddResource(type, setting.RegenAmount);
                            _nextRegenTimes[type] = _nextRegenTimes[type].AddSeconds(setting.IntervalSeconds);
                            _saveSystem.Save(REGEN_NEXT_TIME_KEY_PREFIX + type, _nextRegenTimes[type].Ticks);
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
                ResourceType type = setting.key; 

                if (_activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled)
                {
                    if (_resourceService.IsAtMaxStack(type)) continue;

                    DateTime nextTime = _nextRegenTimes[type];
                    if (now >= nextTime)
                    {
                        TimeSpan overdue = now - nextTime;
                        int extraCycles = (int)(overdue.TotalSeconds / setting.IntervalSeconds) + 1;

                        long totalRegenAmount = extraCycles * setting.RegenAmount;
                        _resourceService.AddResource(type, totalRegenAmount);
                        _nextRegenTimes[type] = nextTime.AddSeconds(extraCycles * setting.IntervalSeconds);
                        _saveSystem.Save(REGEN_NEXT_TIME_KEY_PREFIX + type, _nextRegenTimes[type].Ticks);
                    }
                }
            }
        }

        #region PublicAPI

        public void SetRegenStatus(ResourceType type, bool isEnabled)
        {
            if (_activeStatuses.ContainsKey(type))
            {
                _activeStatuses[type] = isEnabled;
                _saveSystem.Save(REGEN_STATUS_KEY_PREFIX + type, isEnabled);
                
                if (isEnabled)
                {
                    var setting = _resourceConfig.GetResourceData(type);
                    if (setting != null && setting.HasRegen)
                    {
                        _nextRegenTimes[type] = DateTime.UtcNow.AddSeconds(setting.IntervalSeconds);
                        _saveSystem.Save(REGEN_NEXT_TIME_KEY_PREFIX + type, _nextRegenTimes[type].Ticks);
                    }
                }
            }
        }

        public bool IsRegenEnabled(ResourceType type)
        {
            return _activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled;
        }

        public void SetRegenAmount(ResourceType type, long newAmount)
        {
            var data = _resourceConfig.GetResourceData(type);
            if (data != null && data.HasRegen)
            {
                data.RegenAmount = newAmount;
            }
        }

        public DateTime GetNextRegenTime(ResourceType type)
        {
            return _nextRegenTimes.TryGetValue(type, out var time) ? time : DateTime.UtcNow;
        }

        public void SaveAllRegenTimes()
        {
            if (_resourceConfig == null || _saveSystem == null) return;

            foreach (var setting in _resourceConfig.GetAllRegenSettings())
            {
                ResourceType type = setting.key;
              
                if (_activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled)
                {
                    if (_nextRegenTimes.TryGetValue(type, out DateTime nextTime))
                    {
                        _saveSystem.Save(REGEN_NEXT_TIME_KEY_PREFIX + type, nextTime.Ticks);
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