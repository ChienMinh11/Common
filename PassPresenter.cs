namespace ChieChie.GamePass
{
    public class PassPresenter
    {
        private readonly PassModel _model;
        private IPassView _view;

        public PassPresenter(PassModel model)
        {
            _model = model;
            _model.OnDataChanged += HandleModelDataChanged;
        }

        public void BindView(IPassView view)
        {
            if (view == null) return;

            CleanUpDestroyedView();
            if (ReferenceEquals(_view, view)) return;

            UnbindCurrentView();

            _view = view;
            _view.OnClaimRewardClicked += HandleClaimReward;
            _view.OnClaimBonusClicked += HandleClaimBonus;
            _view.OnClaimBonusBankClicked += HandleClaimBonusBank;
            _view.OnBuyPremiumClicked += HandleBuyPremium;

            RefreshView(_model.GetViewData(_view.ViewId));
        }

        public void UnbindView(IPassView view)
        {
            if (!ReferenceEquals(_view, view)) return;

            UnbindCurrentView();
        }

        public void ForceUpdateUI()
        {
            CleanUpDestroyedView();
            if (_view == null) return;

            RefreshView(_model.GetCurrentViewData());
        }

        public void RefreshView(string viewId, PassViewData viewData)
        {
            CleanUpDestroyedView();
            if (_view == null || _view.ViewId != viewId) return;

            RefreshView(viewData);
        }

        private void HandleModelDataChanged()
        {
            CleanUpDestroyedView();
            if (_view == null) return;

            RefreshView(_model.HasDelayedUIUpdate
                ? _model.GetViewData(_view.ViewId)
                : _model.GetCurrentViewData());
        }

        private void RefreshView(PassViewData viewData)
        {
            if (_view == null || viewData == null) return;

            _view.RefreshUI(viewData);
        }

        private void HandleClaimReward(int index, bool isPremium)
        {
            _model.ClaimReward(index, isPremium);
        }

        private void HandleClaimBonus(int index)
        {
            _model.ClaimBonusReward(index);
        }

        private void HandleClaimBonusBank()
        {
            _model.ClaimBonusBankReward();
        }

        private void HandleBuyPremium()
        {
            _model.UnlockPremium();
        }

        private void CleanUpDestroyedView()
        {
            if (_view == null) return;

            if (_view is UnityEngine.MonoBehaviour mb && mb == null)
            {
                _view = null;
            }
        }

        private void UnbindCurrentView()
        {
            if (_view == null) return;

            _view.OnClaimRewardClicked -= HandleClaimReward;
            _view.OnClaimBonusClicked -= HandleClaimBonus;
            _view.OnClaimBonusBankClicked -= HandleClaimBonusBank;
            _view.OnBuyPremiumClicked -= HandleBuyPremium;
            _view = null;
        }

        public void Cleanup()
        {
            _model.OnDataChanged -= HandleModelDataChanged;
            UnbindCurrentView();
        }
    }
}
