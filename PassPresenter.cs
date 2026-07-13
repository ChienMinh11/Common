using ChieChie.MVP;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassPresenter : BasePresenter<IPassView, PassModel>
    {
        public PassPresenter(IPassView view, PassModel model) : base(view, model)
        {
        }

        public bool IsBoundTo(IPassView view)
        {
            return ReferenceEquals(View, view);
        }

        protected override void OnInitialize()
        {
            Model.OnDataChanged += HandleModelDataChanged;
            View.OnClaimRewardClicked += HandleClaimReward;
            View.OnClaimBonusClicked += HandleClaimBonus;
            View.OnClaimBonusBankClicked += HandleClaimBonusBank;
            View.OnBuyPremiumClicked += HandleBuyPremium;

            RefreshView(Model.GetViewData(View.ViewId));
        }

        protected override void OnDispose()
        {
            Model.OnDataChanged -= HandleModelDataChanged;
            View.OnClaimRewardClicked -= HandleClaimReward;
            View.OnClaimBonusClicked -= HandleClaimBonus;
            View.OnClaimBonusBankClicked -= HandleClaimBonusBank;
            View.OnBuyPremiumClicked -= HandleBuyPremium;
        }

        public void ForceUpdateUI()
        {
            if (IsViewDestroyed()) return;

            RefreshView(Model.GetCurrentViewData());
        }

        public PassViewData FlushDelayedUIUpdate()
        {
            if (IsViewDestroyed()) return null;

            var viewData = Model.FlushDelayedUIUpdate(View.ViewId);
            RefreshView(viewData);
            return viewData;
        }

        public void RefreshView(string viewId, PassViewData viewData)
        {
            if (IsViewDestroyed() || View.ViewId != viewId) return;

            RefreshView(viewData);
        }

        private void HandleModelDataChanged()
        {
            if (IsViewDestroyed()) return;

            RefreshView(Model.HasDelayedUIUpdate
                ? Model.GetViewData(View.ViewId)
                : Model.GetCurrentViewData());
        }

        private void RefreshView(PassViewData viewData)
        {
            if (IsViewDestroyed() || viewData == null) return;

            View.RefreshUI(viewData);
        }

        private void HandleClaimReward(int index, bool isPremium)
        {
            Model.ClaimReward(index, isPremium);
        }

        private void HandleClaimBonus(int index)
        {
            Model.ClaimBonusReward(index);
        }

        private void HandleClaimBonusBank()
        {
            Model.ClaimBonusBankReward();
        }

        private void HandleBuyPremium()
        {
            Model.UnlockPremium();
        }

        private bool IsViewDestroyed()
        {
            return View is MonoBehaviour monoBehaviour && monoBehaviour == null;
        }
    }
}
