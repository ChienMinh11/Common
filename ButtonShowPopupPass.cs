using ChieChie.Core;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class ButtonShowPopupPass : MonoBehaviour
    {
        [SerializeField] private bool noFade;

        private IPopupService _popupService;

        public bool NoFade => noFade;

        [Inject]
        private void Contructor(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public void SetNoFade(bool value)
        {
            noFade = value;
        }

        public void OnShowPopup()
        {
            _popupService.ShowPopup("PopupGamePass", noFade);
        }
    }
}
