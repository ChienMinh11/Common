using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.GamePass
{
    public class PassPresenter
    {
        private readonly PassModel _model;
        private readonly PassDatabase _database;
        private readonly List<IPassView> _activeViews = new List<IPassView>();

        public PassPresenter(PassModel model, PassDatabase database)
        {
           _model = model;
           _database = database;
           Initialize();
        }

        public void Initialize()
        {
            _model.OnDataChanged += HandleModelDataChanged;
        }

        public void RegisterView(IPassView view)
        {
            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
                // Đăng ký nhận sự kiện click tương tác từ View
                view.OnClaimRewardClicked += HandleClaimReward;
                view.OnClaimBonusClicked += HandleClaimBonus;
                view.OnBuyPremiumClicked += HandleBuyPremium;
                
                // Cập nhật giao diện lập tức cho View mới mở
                view.RefreshUI(UpdateViewData());
            }
        }

        public void UnregisterView(IPassView view)
        {
            if (_activeViews.Contains(view))
            {
                view.OnClaimRewardClicked -= HandleClaimReward;
                view.OnClaimBonusClicked -= HandleClaimBonus;
                view.OnBuyPremiumClicked -= HandleBuyPremium;
                _activeViews.Remove(view);
            }
        }

        private void HandleModelDataChanged()
        {
            var freshData = UpdateViewData();
            foreach (var view in _activeViews)
            {
                view.RefreshUI(freshData);
            }
        }

        // Hàm đóng gói dữ liệu tinh khiết chuyển xuống cho View hiển thị
        private PassViewData UpdateViewData()
        {
            var viewData = new PassViewData
            {
                CurrentExp = _model.CurrentExp,
                IsPremiumUnlocked = _model.IsPremiumUnlocked,
                CurrentMilestoneIndex = _model.GetCurrentMilestoneIndex(),
                RemainingTimeStr = "Còn 15 ngày", // Ví dụ format chuỗi thời gian từ Scheduler
                Milestones = new List<MilestoneUIData>(),
                AvailableBonusClaims = _model.GetAvailableBonusClaims()
            };

            // Điền dữ liệu cho từng mốc
            foreach (var item in _database.PassItems)
            {
                var milestoneUI = new MilestoneUIData
                {
                    Index = item.index,
                    RequiredExp = item.requiredAmount,
                    FreeRewards = item.freePassrewards,
                    PremiumRewards = item.premiumPassrewards,
                    FreeState = _model.GetMilestoneState(item.index, isPremium: false),
                    PremiumState = _model.GetMilestoneState(item.index, isPremium: true)
                };
                viewData.Milestones.Add(milestoneUI);
            }

            // Điền dữ liệu mốc lặp lại (Bonus)
            if (_database.BonusPassItems.Count > 0)
            {
                viewData.BonusProgressMax = _database.BonusPassItems[0].requiredAmount;
                viewData.BonusProgressCurrent = _model.GetBonusExp() % viewData.BonusProgressMax;
            }

            return viewData;
        }

        // --- Xử lý sự kiện từ UI ---
        private void HandleClaimReward(int index, bool isPremium)
        {
            var itemData = isPremium 
                ? _database.PassItems[index].premiumPassrewards 
                : _database.PassItems[index].freePassrewards;

            if (_model.ClaimReward(index, isPremium))
            {
                // Gọi hiệu ứng nhận quà trực tiếp trên view kích hoạt event
                // Thực tế bạn có thể gửi thông báo cộng Item vào Inventory tại đây
            }
        }

        private void HandleClaimBonus()
        {
            if (_model.ClaimBonusReward())
            {
                // Thao tác cộng quà tương tự
            }
        }

        private void HandleBuyPremium()
        {
            // Gọi SDK IAP mua Gold Pass/Premium Pass ở đây thành công thì:
            _model.UnlockPremium();
        }

        private void CleanUpDestroyedViews()
        {
            _activeViews.RemoveAll(view => 
                view == null || (view is MonoBehaviour mb && mb == null)
            );
        }

        public void Cleanup()
        {
            _model.OnDataChanged -= HandleModelDataChanged;
            _activeViews.Clear();
        }
    }
}