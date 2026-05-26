using System;
using UnityEngine;

namespace MyFramework
{
    [Serializable]
    public class SavedInfiniteResourceData
    {
        public ResourceType ResourceType;
        public float Duration;
        public long StartTimeTicks; // Lưu timestamp dạng ticks
        public long LastValidationTicks;

        public SavedInfiniteResourceData(ResourceType resourceType, float duration, long startTimeTicks, long lastValidationTicks = 0)
        {
            ResourceType = resourceType;
            Duration = duration;
            StartTimeTicks = startTimeTicks;
            LastValidationTicks = lastValidationTicks;
        }
    }
    public class InfiniteResourceData
    {
        public ResourceType ResourceType { get; }
        public float Duration { get; private set; }
        public long StartTimeTicks { get; private set; }
        public long LastValidationTicks { get; set; }
        
        
        // Convert ticks to seconds for remaining time calculation
        private const float TICKS_TO_SECONDS = 1f / TimeSpan.TicksPerSecond;
        
        public bool IsActive
        {
            get
            {
                return RemainingTime > 0;
            }
        }

        public float RemainingTime
        {
            get
            {
                long currentTicks = DateTime.Now.Ticks;
                float elapsedSeconds = (currentTicks - StartTimeTicks) * TICKS_TO_SECONDS;
            
                // Kiểm tra nếu thời gian bị nhảy bất thường (quá xa so với lần validation cuối)
                if (LastValidationTicks > 0)
                {
                    float timeSinceLastValidation = (currentTicks - LastValidationTicks) * TICKS_TO_SECONDS;
                
                    // Nếu thời gian nhảy quá nhiều (ví dụ > 7 ngày) thì có thể là do user thay đổi thời gian
                    if (Math.Abs(timeSinceLastValidation) > 604800) // 7 ngày = 604800 giây
                    {
                        // Reset về thời gian còn lại trước đó
                        float remainingBeforeJump = Duration - ((LastValidationTicks - StartTimeTicks) * TICKS_TO_SECONDS);
                        if (remainingBeforeJump > 0)
                        {
                            StartTimeTicks = currentTicks - (long)((Duration - remainingBeforeJump) / TICKS_TO_SECONDS);
                            elapsedSeconds = (Duration - remainingBeforeJump);
                        }
                    }
                }
            
                LastValidationTicks = currentTicks;
            
                return Math.Max(0, Duration - elapsedSeconds);
            }
        }
        public void ValidateAndResetIfNeeded()
        {
            long currentTicks = DateTime.Now.Ticks;
            float elapsedSeconds = (currentTicks - StartTimeTicks) * TICKS_TO_SECONDS;
    
            // Nếu thời gian bị thay đổi bất thường (âm hoặc quá lớn)
            if (elapsedSeconds < 0 || elapsedSeconds > Duration + 3600) // +1 giờ tolerance
            {
                StartTimeTicks = DateTime.Now.Ticks;
            }
        }

        public InfiniteResourceData(ResourceType resourceType, float duration, long? startTimeTicks = null)
        {
            ResourceType = resourceType;
            Duration = duration;
            StartTimeTicks = startTimeTicks ?? DateTime.Now.Ticks;
            LastValidationTicks = DateTime.Now.Ticks;
        }
        public void ExtendDuration(float additionalDuration)
        {
            float remainingTime = RemainingTime;
            Duration = remainingTime + additionalDuration;
            StartTimeTicks = DateTime.Now.Ticks;
            LastValidationTicks = DateTime.Now.Ticks;
        }
    }
}