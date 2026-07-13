using ChieChie.Core;
using ChieChie.GamePass;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class PopupGamePass : PopupBase
    {
        [SerializeField] private GamePassView gamePassView;

        private PassModel _passModel;
        private IEventService _eventService;

        [Inject]
        private void Construct(PassModel passModel, IEventService eventService)
        {
            _passModel = passModel;
            _eventService = eventService;
            if (gamePassView == null) gamePassView = GetComponent<GamePassView>();
            gamePassView?.Initialize(_passModel,_eventService);
        }
        protected override void SetPopupName() => PopupName = "PopupGamePass";
      
        protected override void SetCacheable() => IsCache = true;
      

        protected override void OnShow()
        {
            if (gamePassView != null)
            {
                gamePassView.RunScrollFirstTimeOpenPopup();
                gamePassView.RefreshUIManual();
            }
        }

        protected override void OnHide()
        {
          
        }

        protected override bool CheckAutoShow()
        {
            return false;
        }

        protected override void Unload()
        {
          
        }

        public void OnCloseClicked()
        {
            OnClose();
        }
    }
}
