using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ChieChie.Core.Utilities
{
    public static class ScrollRectExtensions
    {
  
        public static void FocusOnItem(this ScrollRect scrollRect, RectTransform target, float bias = 0.5f)
        {
            if (scrollRect == null || target == null || scrollRect.content == null) return;
            Canvas.ForceUpdateCanvases();
            CalculateTargetNormalizedPositions(scrollRect, target, bias, out float targetHorizontalPos, out float targetVerticalPos);
            if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = targetHorizontalPos;
            if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = targetVerticalPos;
        }

        public static async UniTask FocusOnItemAsync(this ScrollRect scrollRect, RectTransform target, float duration, CancellationToken cancellationToken, float bias = 0.5f)
        {
            if (scrollRect == null || target == null || scrollRect.content == null) return;
            await UniTask.WaitForEndOfFrame(scrollRect);
            cancellationToken.ThrowIfCancellationRequested();
            Canvas.ForceUpdateCanvases();
            float startHorizontalPos = scrollRect.horizontalNormalizedPosition;
            float startVerticalPos = scrollRect.verticalNormalizedPosition;
            CalculateTargetNormalizedPositions(scrollRect, target, bias, out float targetHorizontalPos, out float targetVerticalPos);

            if (duration <= 0f)
            {
                if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = targetHorizontalPos;
                if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = targetVerticalPos;
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);

                if (scrollRect.horizontal)
                    scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startHorizontalPos, targetHorizontalPos, t);
                
                if (scrollRect.vertical)
                    scrollRect.verticalNormalizedPosition = Mathf.Lerp(startVerticalPos, targetVerticalPos, t);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = targetHorizontalPos;
            if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = targetVerticalPos;
        }

        private static void CalculateTargetNormalizedPositions(ScrollRect scrollRect, RectTransform target, float bias, out float horizontalPos, out float verticalPos)
        {
            Vector2 targetPositionInContent = scrollRect.content.InverseTransformPoint(target.position);
            Vector2 contentSize = scrollRect.content.rect.size;
            Vector2 viewportSize = scrollRect.viewport != null ? scrollRect.viewport.rect.size : ((RectTransform)scrollRect.transform).rect.size;

            horizontalPos = scrollRect.horizontalNormalizedPosition;
            verticalPos = scrollRect.verticalNormalizedPosition;

            if (scrollRect.horizontal && contentSize.x > viewportSize.x)
            {
                float minX = viewportSize.x * bias;
                float currentX = -targetPositionInContent.x;
                float clampX = Mathf.Clamp(currentX + minX, 0, contentSize.x - viewportSize.x);
                horizontalPos = clampX / (contentSize.x - viewportSize.x);
            }

            if (scrollRect.vertical && contentSize.y > viewportSize.y)
            {
                float maxScrollRange = contentSize.y - viewportSize.y;
                float targetYInContent = targetPositionInContent.y;
                float viewportOffset = viewportSize.y * bias;
                float clampedTargetY = Mathf.Clamp(-targetYInContent - viewportOffset, 0f, maxScrollRange);
                verticalPos = 1f - (clampedTargetY / maxScrollRange);
            }
        }
    }
}