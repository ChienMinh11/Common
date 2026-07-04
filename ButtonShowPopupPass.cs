using ChieChie.Core;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class ButtonShowPopupPass : MonoBehaviour
    {
        private IPopupService _popupService;
        [Inject]
        private void Contructor(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public void OnShowPopup()
        {
            _popupService.ShowPopup("PopupGamePass","",false,true);
        }
    }
}
