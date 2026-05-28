using System;
using System.Collections.Generic;

namespace ChieChie.Core
{
    public class ResourceRegenModel
    {
        private const string REGEN_KEY_PREFIX = "Resource_Regen_Next_";
        private readonly ISaveSystem _saveSystem;
        private readonly IResourceManager _resourceManager;
        private readonly ResourceRegenConfig _regenConfig;
        
        // Lưu mốc thời gian UTC của lần hồi phục kế tiếp cho từng tài nguyên
        private readonly Dictionary<ResourceType, DateTime> _nextRegenTimes = new();

        public ResourceRegenModel(ISaveSystem saveSystem, IResourceManager resourceManager, ResourceRegenConfig regenConfig)
        {
            _saveSystem = saveSystem;
            _resourceManager = resourceManager;
            _regenConfig = regenConfig;
        }

        public void Initialize(ResourceConfig resourceConfig)
        {
            _regenConfig.Initialize();
            var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
            DateTime now = DateTime.UtcNow;

            foreach (var type in resourceTypes)
            {
                var regenData = _regenConfig.GetRegenData(type);
                if (regenData == null || !regenData.isRegenEnabled) continue;

                string key = REGEN_KEY_PREFIX + type.ToString();
                _saveSystem.RegisterKey(key);

                long savedTicks = _saveSystem.Load<long>(key, 0L);
                DateTime nextRegen = savedTicks > 0 ? new DateTime(savedTicks, DateTimeKind.Utc) : DateTime.MinValue;

                _nextRegenTimes[type] = nextRegen;

                // Xử lý Offline Regeneration ngay khi đăng nhập
                CalculateOfflineRegen(type, regenData, resourceConfig, now);
            }
        }

        private void CalculateOfflineRegen(ResourceType type, ResourceRegenData regenData, ResourceConfig resourceConfig, DateTime now)
        {
            if (_resourceManager.IsAtMaxStack(type))
            {
                _nextRegenTimes[type] = DateTime.MinValue;
                _saveSystem.Save(REGEN_KEY_PREFIX + type.ToString(), 0L);
                return;
            }

            DateTime nextRegen = _nextRegenTimes[type];
            // Nếu chưa từng có mốc thời gian (ví dụ mới bị mất điểm đầu tiên), đặt mốc là bây giờ + interval
            if (nextRegen == DateTime.MinValue || nextRegen <= now.AddMinutes(-regenData.regenIntervalMinutes * 10))
            {
                _nextRegenTimes[type] = now.AddMinutes(regenData.regenIntervalMinutes);
                _saveSystem.Save(REGEN_KEY_PREFIX + type.ToString(), _nextRegenTimes[type].Ticks);
                return;
            }

            // Nếu thời gian hiện tại đã vượt qua mốc hồi phục tiếp theo (Người chơi offline)
            if (now >= nextRegen)
            {
                TimeSpan totalOfflineTime = now - nextRegen;
                TimeSpan interval = TimeSpan.FromMinutes(regenData.regenIntervalMinutes);
                
                // Tính số lần được hồi phục trong thời gian qua
                long regenCycles = 1 + (long)(totalOfflineTime.TotalMinutes / regenData.regenIntervalMinutes);
                long totalRegenAmount = regenCycles * regenData.regenAmountPerInterval;

                var data = resourceConfig.GetResourceData(type);
                long maxStack = data?.MaxStack ?? 0;
                long currentAmount = _resourceManager.GetCurrentAmount(type);

                if (maxStack > 0 && currentAmount + totalRegenAmount >= maxStack)
                {
                    // Hồi phục đầy max stack
                    long needed = maxStack - currentAmount;
                    if (needed > 0) _resourceManager.AddResource(type, needed);
                    _nextRegenTimes[type] = DateTime.MinValue;
                }
                else
                {
                    _resourceManager.AddResource(type, totalRegenAmount);
                    // Mốc thời gian hồi phục tiếp theo của chu kỳ dở dang
                    double remainingMinutes = regenData.regenIntervalMinutes - (totalOfflineTime.TotalMinutes % regenData.regenIntervalMinutes);
                    _nextRegenTimes[type] = now.AddMinutes(remainingMinutes);
                }

                _saveSystem.Save(REGEN_KEY_PREFIX + type.ToString(), _nextRegenTimes[type].Ticks);
            }
        }

        public void UpdateRuntimeRegen(ResourceType type, DateTime now, ResourceConfig resourceConfig)
        {
            var regenData = _regenConfig.GetRegenData(type);
            if (regenData == null || !regenData.isRegenEnabled) return;

            if (_resourceManager.IsAtMaxStack(type))
            {
                if (_nextRegenTimes[type] != DateTime.MinValue)
                {
                    _nextRegenTimes[type] = DateTime.MinValue;
                    _saveSystem.Save(REGEN_KEY_PREFIX + type.ToString(), 0L);
                }
                return;
            }

            // Nếu đang hụt tài nguyên nhưng chưa kích hoạt mốc thời gian hồi (do vừa xài xong)
            if (_nextRegenTimes[type] == DateTime.MinValue)
            {
                _nextRegenTimes[type] = now.AddMinutes(regenData.regenIntervalMinutes);
                _saveSystem.Save(REGEN_KEY_PREFIX + type.ToString(), _nextRegenTimes[type].Ticks);
                return;
            }

            // Kích hoạt hồi phục trong Runtime khi chạm mốc đếm ngược
            if (now >= _nextRegenTimes[type])
            {
                _resourceManager.AddResource(type, regenData.regenAmountPerInterval);

                if (_resourceManager.IsAtMaxStack(type))
                {
                    _nextRegenTimes[type] = DateTime.MinValue;
                }
                else
                {
                    _nextRegenTimes[type] = now.AddMinutes(regenData.regenIntervalMinutes);
                }
                _saveSystem.Save(REGEN_KEY_PREFIX + type.ToString(), _nextRegenTimes[type].Ticks);
            }
        }

        public TimeSpan GetRemainingRegenTime(ResourceType type, DateTime now)
        {
            if (!_nextRegenTimes.TryGetValue(type, out var nextRegen) || nextRegen == DateTime.MinValue) 
                return TimeSpan.Zero;
                
            return nextRegen <= now ? TimeSpan.Zero : nextRegen - now;
        }
    }
}