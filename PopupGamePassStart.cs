using ChieChie.GamePass;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class PopupGamePassStart : PopupBase
    {
        private IPassService _passService;

        [Inject]
        private void Contructor(IPassService passService)
        {
            _passService = passService;
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
    }
}
