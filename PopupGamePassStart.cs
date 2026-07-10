using UnityEngine;

namespace Game.GamePlay
{
    public class PopupGamePassStart : PopupBase
    {
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
            return true;
        }

        protected override void Unload()
        {
           
        }
    }
}
