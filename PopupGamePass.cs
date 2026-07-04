using ChieChie.GamePass;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class PopupGamePass : PopupBase
    {
        [SerializeField] private GamePassView gamePassView;

        private IPassService _passService;

        [Inject]
        private void Construct(IPassService passService)
        {
            _passService = passService;
            if (gamePassView == null) gamePassView = GetComponent<GamePassView>();
            gamePassView?.Initialize(_passService);
        }
        protected override void SetPopupName() => PopupName = "PopupGamePass";
      
        protected override void SetCacheable() => IsCache = true;
      

        protected override void OnShow()
        {
            if(gamePassView != null) gamePassView.RefreshUIManual();
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
