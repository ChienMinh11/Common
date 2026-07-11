using ChieChie.GamePass;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class PopupGamePassEnd : PopupBase
    {
        protected override void SetPopupName() => PopupName = "PopupGamePassEnd";
        protected override void SetCacheable() => IsCache = false;
        
        private IPassService _passService;

        [Inject]
        private void Contructor(IPassService passService)
        {
            _passService = passService;
        }
        protected override void OnShow()
        {
            
        }

        protected override void OnHide()
        {
           
        }

        protected override bool CheckAutoShow()
        {
            return true;
        }

        protected override void Unload()
        {
           
        }

        public void OnCheckUpdateWhenEventEnd()
        {
            OnClose();
            _passService.CheckEventUpdate();
        }
    }
}
