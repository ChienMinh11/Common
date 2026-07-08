using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class MilestoneHighlight : MonoBehaviour
    {
        [SerializeField] private Image lineTop;
        [SerializeField] private Image lineBottom;

        private CancellationTokenSource _animationCts;

        public void SetDefault() 
        {
            CancelCurrentAnimation();
            
            if (lineTop != null) lineTop.fillAmount = 0f;
            if (lineBottom != null) lineBottom.fillAmount = 0f;
        }

        public void SetImmediate(float value)
        {
            CancelCurrentAnimation();

            value = Mathf.Clamp01(value);
            if (lineTop != null) lineTop.fillAmount = value;
            if (lineBottom != null) lineBottom.fillAmount = value;
        }

        public async UniTask SetByAnimationAsync(float targetValue, float duration = 0.5f)
        {
            CancelCurrentAnimation();
            _animationCts = new CancellationTokenSource();
            var token = _animationCts.Token;

            targetValue = Mathf.Clamp01(targetValue);
            
            float startValueTop = lineTop != null ? lineTop.fillAmount : 0f;
            float startValueBottom = lineBottom != null ? lineBottom.fillAmount : 0f;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    if (token.IsCancellationRequested) return;

                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;

                    if (lineTop != null) 
                        lineTop.fillAmount = Mathf.Lerp(startValueTop, targetValue, progress);
                    
                    if (lineBottom != null) 
                        lineBottom.fillAmount = Mathf.Lerp(startValueBottom, targetValue, progress);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                if (lineTop != null) lineTop.fillAmount = targetValue;
                if (lineBottom != null) lineBottom.fillAmount = targetValue;
            }
            catch (OperationCanceledException)
            {
              
            }
        }

        private void CancelCurrentAnimation()
        {
            if (_animationCts != null)
            {
                _animationCts.Cancel();
                _animationCts.Dispose();
                _animationCts = null;
            }
        }

        private void OnDestroy()
        {
          
            CancelCurrentAnimation();
        }
    }
}