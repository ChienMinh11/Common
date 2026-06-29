using ChieChie.Core;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class ButtonShowPopupProfile : MonoBehaviour
    {
        private IPopupService _popupService;
        [Inject]
        private void Contruct(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public void OnShowPopup()
        {
            _popupService.ShowPopup("PopupProfile");
        }
    }
}
