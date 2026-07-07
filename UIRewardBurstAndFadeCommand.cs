using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Game.GamePlay
{
    public class UIRewardBurstAndFadeCommand : IEffectCommand
    {
        private readonly EffectBurstConfig _config;
        private readonly Transform _uiParent;
        private readonly Vector2 _spawnAnchoredPos;
        private readonly IEffectPlaybackService _playbackService;
        private readonly List<RewardClaimedEventData> _rewardsToSpawn;
        private readonly Action<string> _onRewardCompleted; 

        private readonly float _moveUpDistance = 150f; 
        private readonly float _fadeDuration = 0.8f; 
        private readonly Ease _fadeEase = Ease.OutQuad;

        public UIRewardBurstAndFadeCommand(
            IEffectPlaybackService playbackService, 
            EffectBurstConfig config, 
            Transform uiParent, 
            Vector2 spawnAnchoredPos, 
            List<RewardClaimedEventData> rewardsToSpawn,
            Action<string> onRewardCompleted = null) 
        {
            _playbackService = playbackService;
            _config = config;
            _uiParent = uiParent;
            _spawnAnchoredPos = spawnAnchoredPos;
            _rewardsToSpawn = rewardsToSpawn;
            _onRewardCompleted = onRewardCompleted;
        }

        public async UniTask ExecuteAsync(EffectContext context, CancellationToken token)
        {
            if (_config == null || context == null || _rewardsToSpawn == null || _rewardsToSpawn.Count == 0) return;
            
            int totalCount = _rewardsToSpawn.Count;
            _playbackService.PlaySound(_config.objbustSound);
            
            float configDelayBetween = 0.2f;

            float minRandomX = -70f;
            float maxRandomX = 70f;

            List<(RewardEffectView view, string resourceKey)> spawnedEffects = new List<(RewardEffectView, string)>();
            List<UniTask> spawnTasks = new List<UniTask>();

            for (int i = 0; i < totalCount; i++)
            {
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                var effect = _playbackService.GetEffectComponent<RewardEffectView>(_uiParent);
                if (effect != null)
                {
                    var rewardData = _rewardsToSpawn[i];
                    effect.Setup(rewardData.RewardSprite, rewardData.AmountDisplayText, rewardData.ShowAmountText);
   
                    if (effect.TryGetComponent<CanvasGroup>(out var canvasGroup))
                    {
                        canvasGroup.alpha = 1f;
                    }

                    if (effect.transform is RectTransform rectTransform)
                    {
                        float xOffset = UnityEngine.Random.Range(minRandomX, maxRandomX);
            
                        Vector2 spawnTargetPos = _spawnAnchoredPos + new Vector2(xOffset, 0f);
                        rectTransform.anchoredPosition = spawnTargetPos;
                        rectTransform.localScale = Vector3.one * 0.95f;

                        var scaleTask = rectTransform.DOScale(Vector3.one, _config.burstDuration)
                            .SetEase(_config.burstCurve)
                            .SetTimeScaleSpeedBased(context.SpeedMultiplier)
                            .ToUniTask(cancellationToken: token);
                        
                        spawnTasks.Add(scaleTask);
                        spawnedEffects.Add((effect, rewardData.ResourceType));
                    }
                }
            
                if (i < totalCount - 1 && configDelayBetween > 0)
                {
                    float actualDelay = configDelayBetween / context.SpeedMultiplier;
                    int delayMs = (int)(actualDelay * 1000);
                    
                    try
                    {
                        await UniTask.Delay(delayMs, delayType: DelayType.DeltaTime, cancellationToken: token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning($"[RewardCollect] UniTask.Delay bị hủy tại index: {i}");
                        throw;
                    }
                }
            }

            if (spawnTasks.Count > 0)
            {
                await UniTask.WhenAll(spawnTasks);
            }

            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            List<UniTask> fadeTasks = new List<UniTask>();

            for (int i = 0; i < spawnedEffects.Count; i++)
            {
                var item = spawnedEffects[i];
                var effect = item.view;
                string resKey = item.resourceKey;
                if (effect == null || !(effect.transform is RectTransform rectTransform)) continue;

                float itemDelay = _config.delayBeforeGather + (i * 0.1f);

                float targetY = rectTransform.anchoredPosition.y + _moveUpDistance;
                var moveYTask = rectTransform.DOAnchorPosY(targetY, _fadeDuration)
                    .SetEase(_fadeEase)
                    .SetDelay(itemDelay)
                    .SetTimeScaleSpeedBased(context.SpeedMultiplier)
                    .ToUniTask(cancellationToken: token);

                UniTask fadeOutTask = UniTask.CompletedTask;
                if (rectTransform.TryGetComponent<CanvasGroup>(out var canvasGroup))
                {
                    fadeOutTask = canvasGroup.DOFade(0f, _fadeDuration)
                        .SetEase(_fadeEase)
                        .SetDelay(itemDelay)
                        .SetTimeScaleSpeedBased(context.SpeedMultiplier)
                        .ToUniTask(cancellationToken: token);
                }
                else
                {
                    Debug.LogWarning($"[UIRewardBurstAndFadeCommand] Prefab {rectTransform.name} thiếu Component CanvasGroup để làm mờ!");
                }

                async UniTask HandleFadeAnimation(UniTask moveUp, UniTask fade, RewardEffectView view, string key)
                {
                    await UniTask.WhenAll(moveUp, fade);
                    
                    _onRewardCompleted?.Invoke(key);
                    
                    if (view != null && view.gameObject != null)
                    {
                        _playbackService.ReturnEffect(view.gameObject);
                    }
                }
             
                fadeTasks.Add(HandleFadeAnimation(moveYTask, fadeOutTask, effect, resKey));
            }

            if (fadeTasks.Count > 0)
            {
                await UniTask.WhenAll(fadeTasks);
            }
        }
    }
}