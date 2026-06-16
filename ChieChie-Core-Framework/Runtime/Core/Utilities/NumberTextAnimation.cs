using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class NumberAnimationSettings
    {
        [Tooltip("Thời gian cơ bản cho animation khi delta nhỏ hơn speedThreshold")]
        public float baseAnimationDuration = 0.3f;

        [Tooltip("Thời gian tối thiểu cho animation, animation sẽ không chạy nhanh hơn giá trị này")]
        public float minAnimationDuration = 0.3f;

        [Tooltip("Thời gian tối đa cho animation, animation sẽ không chạy chậm hơn giá trị này")]
        public float maxAnimationDuration = 1f;

        [Tooltip("Ngưỡng để bắt đầu tăng tốc animation. Nếu delta > speedThreshold, animation sẽ chạy nhanh hơn")]
        public float speedThreshold = 1000f;

        [Tooltip("Hệ số tăng tốc, càng lớn thì animation càng nhanh với các số lớn. Công thức: speedMultiplier = 1 + accelerationFactor * log10(delta/speedThreshold)")]
        public float accelerationFactor = 0.5f;
    }

    public class NumberTextAnimation
    {
        private readonly NumberAnimationSettings settings;
        private readonly Action<long> onValueChanged;
        
        private long currentValue;
        private long targetValue;
        private float currentSpeed;
    
        private CancellationTokenSource animationCts;

        public NumberTextAnimation(
            NumberAnimationSettings settings,
            Action<long> onValueChanged)
        {
            this.settings = settings;
            this.onValueChanged = onValueChanged;
        }

        public void AnimateTo(long fromValue, long toValue)
        {
            currentValue = fromValue;
            targetValue = toValue;

            Stop();

            animationCts = new CancellationTokenSource();

            AnimateTaskAsync(fromValue, toValue, animationCts.Token).Forget();
        }

        private async UniTaskVoid AnimateTaskAsync(long fromValue, long toValue, CancellationToken cancellationToken)
        {
            float duration = CalculateAnimationDuration(fromValue, toValue);
            float delta = Math.Abs(toValue - fromValue);
            currentSpeed = delta / duration;

            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < duration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    
                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / duration);

                    long currentAmount;
                    if (fromValue < toValue)
                    {
                        currentAmount = fromValue + (long)(delta * progress);
                    }
                    else
                    {
                        currentAmount = fromValue - (long)(delta * progress);
                    }
                    
                    currentValue = currentAmount;
                    onValueChanged?.Invoke(currentAmount);
                }
          
                currentValue = toValue;
                onValueChanged?.Invoke(toValue);
            }
            catch (OperationCanceledException)
            {
               
            }
        }

        private float CalculateAnimationDuration(long fromAmount, long toAmount)
        {
            float delta = Math.Abs(toAmount - fromAmount);
            
            if (delta <= settings.speedThreshold)
            {
                return settings.baseAnimationDuration;
            }
            
            float speedMultiplier = 1f + settings.accelerationFactor * Mathf.Log10(delta / settings.speedThreshold);
            float duration = settings.baseAnimationDuration / speedMultiplier;
            
            return Mathf.Clamp(duration, settings.minAnimationDuration, settings.maxAnimationDuration);
        }

        public void Stop()
        {
            if (animationCts != null)
            {
                animationCts.Cancel();
                animationCts.Dispose();
                animationCts = null;
            }
        }

        public long GetCurrentValue() => currentValue;
        public long GetTargetValue() => targetValue;
    }
}