using ChieChie.Constracts;
using ChieChie.Core;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class ButtonShowPopupShop : MonoBehaviour
    {
        private IPopupService _popupService;
        [Inject]
        private void Construct(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public void OnShowPopupShop()
        {
            _popupService.ShowPopup("PopupTest1");
        }
    }
}
