using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.Core
{
    public abstract class PopupBase : MonoBehaviour,IPopup
    {
        [SerializeField] private TweenUI tweenShowUI;
        [SerializeField] private TweenUI tweenHideUI;
        
        protected IEventService EventService;
        
        protected string PopupName;
        protected bool IsCache;
        public string PopupNameId => PopupName;
        
        public void Initialize(IEventService eventService)
        {
            EventService = eventService;
            SetPopupName();
            SetCacheable();
        }

        public bool IsCacheable => IsCache;
        public bool CanShow()
        {
            return CheckCanShow();
        }

        public async UniTask Show()
        {
            OnShow();
            await tweenShowUI.PlayShowAsync(this.destroyCancellationToken);           
        }
        public async UniTask Hide()
        {
           
            await tweenHideUI.PlayHideAsync(this.destroyCancellationToken);
            OnHide();
        }
    
        protected abstract void SetPopupName();
        protected abstract void SetCacheable();
        protected abstract void OnShow();
        protected abstract void OnHide();
        protected abstract bool CheckCanShow();

        protected virtual void OnClose()
        {
            EventService.Publish<IPopup, PopupEventType>(PopupEventType.OnPopupRequestClose, this);
        }

        private void OnDisable()
        {
            tweenHideUI.Unload();
        }

        private void OnDestroy()
        {
            tweenShowUI.KillAllTweens();
            tweenHideUI.KillAllTweens();
        }
      
    }
}
