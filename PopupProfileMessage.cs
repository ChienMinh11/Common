using ChieChie.Core;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class PopupProfileMessage : PopupBase
    {
        private IPopupService _popupService;

        [Inject]
        private void Construct(IPopupService popupService)
        {
            _popupService = popupService;
        }
        protected override void SetPopupName() => PopupName = "PopupProfileMessage";


        protected override void SetCacheable() => IsCache = false;
       

        protected override void OnShow()
        {
           
        }

        protected override void OnHide()
        {
           
        }

        protected override bool CheckAutoShow()
        {
            return false;
        }

        public void OnClickClose()
        {
            OnCloseAll();
        }

        public void OnClickSaveChange()
        {
            if (_popupService != null)
            {
               
                var profilePopup = _popupService.GetPopup<PopupProfile>("PopupProfile");
                
                if (profilePopup != null)
                {
                    profilePopup.SaveChangesAndClose();
                } 
                OnCloseAll();
            }
        }

        protected override void Unload()
        {
           
        }
    }
}
