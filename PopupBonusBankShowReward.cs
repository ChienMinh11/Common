using System.Collections.Generic;
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
        [SerializeField] private Button claimButton;

        private RewardDisplayService _rewardDisplayService;
        private IEventService _eventService;
        private IEffectSequenceService _effectSequenceService;
   
      
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

        protected override void OnShow()
        {
            
            buttonFade.SetDefautCanvasGroup();
            claimButton.interactable = false;

            var data = _rewardDisplayService.CurrentData;
            if (data == null) return;
            
            var rewards = data.GetRewards();
            var reward = rewards[0];
      
            PlayChestAnimationSequenceAsync().Forget();
        }

        private async UniTaskVoid PlayChestAnimationSequenceAsync()
        {
            var token = this.destroyCancellationToken;

            if (buttonFade != null)
            {
                buttonFade.KillAllTweens();
                if (buttonFade.Rect != null) buttonFade.Rect.localScale = Vector3.zero;
            }

            await PlayTextAmountAnimation();

            if (buttonFade != null)
            {
                await buttonFade.PlayShowAsync(token);
                claimButton.interactable = true;
            }
        }

        private async UniTask PlayTextAmountAnimation()
        {
            
        }

        protected override void OnHide()
        {
            _rewardDisplayService.CurrentData?.OnClosePopup();
            _rewardDisplayService.SetContextData(null);
        }

        public async void OnClickClaimAndClose()
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
            OnClose();
        }
    }
}
