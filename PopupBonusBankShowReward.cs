using System.Collections.Generic;
using System.Threading.Tasks;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Core.ChieChie.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class PopupBonusBankShowReward : PopupBase
    {
        [SerializeField] private TMP_Text amount;

        [SerializeField] private TweenUI buttonFade;
        [SerializeField] private TweenUI rewardDisplay;
        [SerializeField] private Button claimButton;

        private RewardDisplayService _rewardDisplayService;
        private IEventService _eventService;
        private IEffectSequenceService _effectSequenceService;
        private UniTaskCompletionSource _closeTaskSource;
   
        private long _targetAmount = 0;
        [Inject]
        public void Construct(RewardDisplayService rewardDisplayService, IEventService eventService,
            IEffectSequenceService effectSequenceService)
        {
            _rewardDisplayService = rewardDisplayService;
            _eventService = eventService;
            _effectSequenceService = effectSequenceService;
        }

        protected override void SetPopupName() => PopupName = "PopupBonusBankShowReward";
        protected override void SetCacheable() => IsCache = false;

        protected override bool CheckAutoShow() => true;

        public UniTask WaitForClose()
        {
            _closeTaskSource = new UniTaskCompletionSource();
            return _closeTaskSource.Task;
        }
        
        protected override void OnShow()
        {
            if (amount != null)
                amount.text = "0"; 
            rewardDisplay.SetDefaultState();
            buttonFade.SetDefautCanvasGroup();
            claimButton.interactable = false;

            var data = _rewardDisplayService.CurrentData;
            if (data == null) return;
            
            var rewards = data.GetRewards();
            if (rewards != null && rewards.Count > 0)
            {
                _targetAmount = rewards[0].Amount;
            }
            else
            {
                _targetAmount = 0;
            }
           
            if (buttonFade != null)
            {
                buttonFade.KillAllTweens();
                if (buttonFade.Rect != null) buttonFade.Rect.localScale = Vector3.zero;
            }
            
        }

        public void PlayAnim()
        {
            PlayAnimationAsync().Forget();
        }

        private async UniTaskVoid PlayAnimationAsync()
        {
            var token = this.destroyCancellationToken;
            await UniTask.Delay(500);
            await rewardDisplay.PlayShowAsync(token);
            await PlayTextAmountAnimation();
            if (buttonFade != null)
            {
                await buttonFade.PlayShowAsync(token);
                claimButton.interactable = true;
            }
        }

        private async UniTask PlayTextAmountAnimation()
        {
            if (amount == null) return;

            if (_targetAmount <= 0) return;

            float duration = 0.75f; 
            float elapsed = 0f;
            var token = this.destroyCancellationToken;

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                  
                    long currentDisplayValue = (long)Mathf.Lerp(0, _targetAmount, progress);
                
                    amount.text = currentDisplayValue.ToString("N0");

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (System.OperationCanceledException)
            {
              
                return;
            }
       
            amount.text = _targetAmount.ToString("N0");
        }

        protected override void OnHide()
        {
            _rewardDisplayService.CurrentData?.OnClosePopup();
            _rewardDisplayService.SetContextData(null);
          
        }

        public void OnClickClaimAndClose()
        {
            OnClickClaimAndCloseAsync().Forget();
        }

        private async UniTaskVoid OnClickClaimAndCloseAsync()
        {
            var data = _rewardDisplayService.CurrentData;
            if (data != null)
            {
                PublishRewardClaimedEvents(data);
            }
            HideRootCanvas();
            claimButton.interactable = false;
            if (_effectSequenceService != null)
            {
                await UniTask.WaitUntil(() => !_effectSequenceService.IsProcessing, cancellationToken: this.destroyCancellationToken);
            }
            else
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: this.destroyCancellationToken);
            }

            OnClose();
            _closeTaskSource?.TrySetResult();
        }

        private void PublishRewardClaimedEvents(IBaseRewardDisplayData data)
        {
            data.OnRewardsClaimed();
            List<RewardClaimedEventData> rewardDataList = RewardItemDataHelper.FromRewardDisplayData(data);
            _eventService.Publish<List<RewardClaimedEventData>, GameEvent>(
                GameEvent.OnRewardClaimByPopupDisplayReward,
                rewardDataList
            );
        }

        protected override void Unload()
        {
            if (_closeTaskSource != null && !_closeTaskSource.Task.Status.IsCompleted())
            {
                _closeTaskSource.TrySetCanceled();
            }
            OnClose();
        }
    }
}
