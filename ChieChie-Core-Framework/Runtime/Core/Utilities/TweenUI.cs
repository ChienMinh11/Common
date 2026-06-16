using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable]
    public class TweenUI
    {
        public enum AnimType
        {
            Fade,
            Move,
            Scale,
            CombinedFadeWithScale,
            CombinedFadeWithMove,
            CombinedAll
        }

        [Header("--- Cấu hình Tween (ScriptableObject) ---")] 
        [SerializeField] private TweenUIConfig config;

        [Header("--- Thành phần UI Target ---")] 
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("--- Cấu hình Vị trí Move ---")] 
        [SerializeField] private Vector2 hidePosition;
        [SerializeField] private Vector2 showPosition;

        public RectTransform Rect => rectTransform;

        /// <summary>
        /// Thay đổi cấu hình linh hoạt bằng code nếu muốn custom runtime
        /// </summary>
        public void SetCustomConfig(TweenUIConfig customConfig)
        {
            config = customConfig;
        }

        public void Setup(RectTransform rect, CanvasGroup canvas = null)
        {
            rectTransform = rect;
            canvasGroup = canvas;
        }

        public void SetupCanvasGroup(CanvasGroup canvas)
        {
            canvasGroup = canvas;
        }

        public async UniTask PlayShowAsync(CancellationToken lifeTimeToken)
        {
            if (config == null)
            {
                Debug.LogWarning("TweenUI chưa được gán TweenUIConfig!");
                return;
            }

            KillAllTweens();

            switch (config.animationType)
            {
                case AnimType.Fade:
                    if (canvasGroup) canvasGroup.alpha = 0f;
                    await canvasGroup.DOFade(1f, config.duration).SetDelay(config.delay)
                        .ToUniTask(cancellationToken: lifeTimeToken);
                    break;

                case AnimType.Scale:
                    if (rectTransform == null) return;
                    rectTransform.localScale = Vector3.zero;
                    await rectTransform.DOScale(Vector3.one, config.duration).SetEase(config.showCurve).SetDelay(config.delay)
                        .ToUniTask(cancellationToken: lifeTimeToken);
                    break;

                case AnimType.Move:
                    if (rectTransform == null) return;
                    rectTransform.anchoredPosition = hidePosition;
                    await rectTransform.DOAnchorPos(showPosition, config.duration).SetEase(config.showCurve).SetDelay(config.delay)
                        .ToUniTask(cancellationToken: lifeTimeToken);
                    break;

                case AnimType.CombinedFadeWithScale:
                    if (rectTransform == null) return;
                    if (canvasGroup) canvasGroup.alpha = 0f;
                    rectTransform.localScale = Vector3.zero;

                    Sequence showSeq = DOTween.Sequence();
                    if (canvasGroup)
                    {
                        _ = showSeq.Append(canvasGroup.DOFade(1f, config.duration));
                    }

                    _ = showSeq.AppendInterval(0.02f);
                    _ = showSeq.Join(rectTransform.DOScale(Vector3.one, config.duration).SetEase(config.showCurve));

                    await showSeq.ToUniTask(cancellationToken: lifeTimeToken);
                    break;
            }
        }

        public async UniTask PlayHideAsync(CancellationToken lifeTimeToken)
        {
            if (config == null) return;

            KillAllTweens();

            switch (config.animationType)
            {
                case AnimType.Fade:
                    if (canvasGroup)
                    {
                        await canvasGroup.DOFade(0f, config.duration)
                            .ToUniTask(cancellationToken: lifeTimeToken);
                    }
                    break;

                case AnimType.Scale:
                    if (rectTransform == null) return;
                    await rectTransform.DOScale(Vector3.zero, config.duration).SetEase(config.hideCurve)
                        .ToUniTask(cancellationToken: lifeTimeToken);
                    break;

                case AnimType.Move:
                    if (rectTransform == null) return;
                    await rectTransform.DOAnchorPos(hidePosition, config.duration).SetEase(config.hideCurve)
                        .ToUniTask(cancellationToken: lifeTimeToken);
                    break;

                case AnimType.CombinedFadeWithScale:
                    if (rectTransform == null) return;
                    Sequence hideSeq = DOTween.Sequence();
                    _ = hideSeq.Append(rectTransform.DOScale(Vector3.zero, config.duration).SetEase(config.hideCurve));
                    _ = hideSeq.AppendInterval(0.02f);
                    if (canvasGroup)
                    {
                        _ = hideSeq.Join(canvasGroup.DOFade(0f, config.duration));
                    }

                    await hideSeq.ToUniTask(cancellationToken: lifeTimeToken);
                    break;
            }
        }

        public void Unload()
        {
            if (rectTransform != null) rectTransform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        public void KillAllTweens()
        {
            rectTransform?.DOKill();
            canvasGroup?.DOKill();
        }
    }
  
}