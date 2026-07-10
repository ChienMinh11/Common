using System;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public abstract class PopupBase : MonoBehaviour,IPopup
    {
        [SerializeField] protected TweenUI tweenShowUI;
        [SerializeField] protected TweenUI tweenHideUI;
        
        public Action<IPopup> OnClosed { get; set; }
        public Action<IPopup> OnRequestClose { get; set; }
        public Action<IPopup> OnRequestCloseAll { get; set; }
        
        public Action<IPopup> OnHideRootCanvas { get; set; }

        protected string PopupName;
        protected bool IsCache;
        public string PopupNameId => PopupName;
        
        public void Initialize()
        {
            SetPopupName();
            SetCacheable();
        }

        public bool IsCacheable => IsCache;
        public bool CanAutoShow()
        {
            return CheckAutoShow();
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
            OnClosed?.Invoke(this);
        }
    
        protected abstract void SetPopupName();
        protected abstract void SetCacheable();
        protected abstract void OnShow();
        protected abstract void OnHide();
        protected abstract bool CheckAutoShow();

        protected virtual void OnClose()
        {
            OnRequestClose?.Invoke(this);
        }

        protected virtual void OnCloseAll()
        {
            OnRequestCloseAll?.Invoke(this);
        }

        public void ForceClose()
        {
            OnClose();
        }
    

        private void OnDisable()
        {
            tweenHideUI.Unload();
        }

        private void OnDestroy()
        {
            Unload();
            tweenShowUI.KillAllTweens();
            tweenHideUI.KillAllTweens();
        }

        protected void HideRootCanvas()
        {
            OnHideRootCanvas?.Invoke(this);
        }

        protected abstract void Unload();

    }
}
