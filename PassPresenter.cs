using System;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassPresenter : IDisposable
    {
        private readonly IPassView _view;
        private readonly IPassService _passService;
        private bool _isActive;

        public PassPresenter(IPassView view, IPassService passService)
        {
            _view = view;
            _passService = passService;
        }

        public void Initialize()
        {
            if (_isActive) return;
            _isActive = true;
            PassManager.OnPassDataChanged += UpdateView;
            UpdateView();
        }

        public void Disable()
        {
            if (!_isActive) return;
            _isActive = false;

            PassManager.OnPassDataChanged -= UpdateView;
        }

        public void UpdateView()
        {
            if (!_passService.IsInitialized) return;
         
            TimeSpan remaining = _passService.Scheduler.GetRemainingTime(DateTime.UtcNow);
            string timeStr = remaining == TimeSpan.Zero 
                ? "Sự kiện đã kết thúc" 
                : $"{remaining.Days} ngày {remaining.Hours} giờ";

            _view.RefreshPassUI(_passService.Model, _passService.Database, timeStr);
        }

        public void OnClaimRewardClicked(int tierIndex, bool isPremium)
        {
            if (_passService.CanClaimReward(tierIndex, isPremium))
            {
                _passService.ClaimReward(tierIndex, isPremium);
               
            }
        }

        public void OnClaimBonusClicked()
        {
            if (_passService.CanClaimBonus())
            {
                _passService.ClaimBonus();
            }
        }

        public void OnBuyPremiumClicked()
        {
            _passService.BuyPremium();
        }

        public void Dispose()
        {
            Disable();
        }
    }
}