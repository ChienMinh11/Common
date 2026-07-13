using ChieChie.Core;
using ChieChie.GamePass;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class PopupGamePassStart : PopupBase
    {
        private IPassService _passService;
        private IPopupService _popupService;

        [Inject]
        private void Contructor(IPassService passService,IPopupService popupService)
        {
            _passService = passService;
            _popupService = popupService;
        }
        protected override void SetPopupName() => PopupName = "PopupGamePassStart";
        protected override void SetCacheable() => IsCache = false;
        protected override void OnShow()
        {
            
        }

        protected override void OnHide()
        {
           
        }

        protected override bool CheckAutoShow()
        {
            return _passService != null && !_passService.IsEventActive;
        }

        protected override void Unload()
        {
           
        }

        public void OnStartNewEvent()
        {
            _passService.ActiveNewEvent();
            OnClose();
            _popupService.ShowPopup("PopupGamePass");
        }
    }
}
