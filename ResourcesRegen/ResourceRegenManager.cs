using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
    public class ResourceRegenManager : MonoBehaviour, IResourceRegenService, IInitialisable
    {
        private const string REGEN_STATUS_KEY_PREFIX = "Resource_Regen_Enabled_";
        private const string REGEN_LAST_TIME_KEY = "Resource_Regen_LastTick";

        [SerializeField] private ResourceRegenConfig regenConfig;

        private IResourceManager _resourceManager;
        private ISaveSystem _saveSystem;
        private CancellationTokenSource _cts;

        private readonly Dictionary<ResourceType, bool> _activeStatuses = new();
        private readonly Dictionary<ResourceType, float> _timers = new();

        public int InitializationPriority => 10; // Khởi tạo SAU ResourceManager (Priority 0)

        [Inject]
        private void Construct(IResourceManager resourceManager, ISaveSystem saveSystem)
        {
            _resourceManager = resourceManager;
            _saveSystem = saveSystem;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            if (regenConfig != null)
            {
                regenConfig.Initialize();
                
                // Đăng ký các key lưu trữ vào SaveSystem
                foreach (var setting in regenConfig.GetAllRegenSettings())
                {
                    string statusKey = REGEN_STATUS_KEY_PREFIX + setting.resourceType;
                    _saveSystem.RegisterKey(statusKey);
                    
                    // Load trạng thái bật/tắt đã lưu, nếu chưa có thì lấy mặc định từ config
                    bool isEnabled = _saveSystem.Load(statusKey, setting.isEnabledByDefault);
                    _activeStatuses[setting.resourceType] = isEnabled;
                    _timers[setting.resourceType] = 0f;
                }
                
                _saveSystem.RegisterKey(REGEN_LAST_TIME_KEY);

                // Xử lý Hồi phục Ngoại tuyến (Offline Regeneration)
                ProcessOfflineRegen();

                // Bắt đầu vòng lặp cập nhật thời gian thực
                _cts = new CancellationTokenSource();
                StartRegenLoopAsync(_cts.Token).Forget();
            }

            return UniTask.FromResult(true);
        }

        public bool IsInitialized { get; }

        private async UniTaskVoid StartRegenLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Sử dụng DeltaTime của Unity qua PlayerLoopTiming.Update
                float deltaTime = Time.deltaTime; 

                foreach (var setting in regenConfig.GetAllRegenSettings())
                {
                    ResourceType type = setting.resourceType;

                    // KIỂM TRA ĐIỀU KIỆN: Tính năng được bật + Chưa đạt tối đa Stack + Không phải trạng thái Vô hạn
                    if (_activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled)
                    {
                        if (!_resourceManager.IsAtMaxStack(type) && !_resourceManager.IsCurrentlyInfinite(type))
                        {
                            _timers[type] += deltaTime;

                            if (_timers[type] >= setting.intervalSeconds)
                            {
                                // Kích hoạt thêm tài nguyên
                                _resourceManager.AddResource(type, setting.regenAmount);
                                _timers[type] = 0f; // Reset timer
                            }
                        }
                    }
                }

                // Lưu dấu mốc thời gian hiện tại định kỳ để tính offline
                _saveSystem.Save(REGEN_LAST_TIME_KEY, DateTime.UtcNow.Ticks);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private void ProcessOfflineRegen()
        {
            long lastTick = _saveSystem.Load<long>(REGEN_LAST_TIME_KEY, 0L);
            if (lastTick <= 0) return;

            DateTime lastTime = new DateTime(lastTick, DateTimeKind.Utc);
            TimeSpan offlineDuration = DateTime.UtcNow - lastTime;

            if (offlineDuration <= TimeSpan.Zero) return;

            double totalSecondsOffline = offlineDuration.TotalSeconds;

            foreach (var setting in regenConfig.GetAllRegenSettings())
            {
                ResourceType type = setting.resourceType;

                if (_activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled)
                {
                    // Chỉ tính offline nếu tài nguyên chưa đầy
                    if (!_resourceManager.IsAtMaxStack(type))
                    {
                        int regenCycles = (int)(totalSecondsOffline / setting.intervalSeconds);
                        if (regenCycles > 0)
                        {
                            long totalOfflineRegenAmount = regenCycles * setting.regenAmount;
                            
                            // ResourceManager.AddResource() bên trong đã tự giới hạn theo MaxStack của Model
                            _resourceManager.AddResource(type, totalOfflineRegenAmount);
                        }
                    }
                }
            }
        }

        #region IResourceRegenService Implementation

        public void SetRegenStatus(ResourceType type, bool isEnabled)
        {
            if (_activeStatuses.ContainsKey(type))
            {
                _activeStatuses[type] = isEnabled;
                _saveSystem.Save(REGEN_STATUS_KEY_PREFIX + type, isEnabled);
                
                if (!isEnabled) _timers[type] = 0f; // Reset timer nếu tắt đi
            }
        }

        public bool IsRegenEnabled(ResourceType type)
        {
            return _activeStatuses.TryGetValue(type, out bool isEnabled) && isEnabled;
        }

        public void SetRegenAmount(ResourceType type, long newAmount)
        {
            var data = regenConfig.GetRegenData(type);
            if (data != null)
            {
                data.regenAmount = newAmount;
            }
        }

        public float GetCurrentTimer(ResourceType type)
        {
            return _timers.TryGetValue(type, out float timer) ? timer : 0f;
        }

        public ResourceRegenConfig GetRegenConfig()
        {
            return regenConfig;
        }

        #endregion

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}