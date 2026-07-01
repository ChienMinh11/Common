using System;
using System.Collections.Generic;
using System.Linq;

namespace ChieChie.GamePass
{
    public class PassPresenter
    {
        private readonly PassModel _model;
        private readonly PassDatabase _database;
        private readonly List<IPassView> _activeViews = new List<IPassView>();

        // --- CACHE VARIABLES ---
        private List<PassBonusData> _sortedBonusItemsCache;
        // -

        public PassPresenter(PassModel model, PassDatabase database)
        {
            _model = model;
            _database = database;
            Initialize();
        }

        private void Initialize()
        {
            _model.OnDataChanged += HandleModelDataChanged;

            // Cache lại danh sách đã sắp xếp ngay từ đầu, loại bỏ OrderBy trong runtime
            _sortedBonusItemsCache = _database.BonusPassItems.OrderBy(b => b.index).ToList();
        }

        public void RegisterView(IPassView view)
        {
            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
                view.OnClaimRewardClicked += HandleClaimReward;
                view.OnClaimBonusClicked += HandleClaimBonus;
                view.OnBuyPremiumClicked += HandleBuyPremium;

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

        private PassViewData UpdateViewData()
        {
            var viewData = new PassViewData
            {
                CurrentExp = _model.CurrentExp,
                IsPremiumUnlocked = _model.IsPremiumUnlocked,
                CurrentMilestoneIndex = _model.GetCurrentMilestoneIndex(),
                RemainingTimeStr = "Event Active",
                Milestones = new List<MilestoneUIData>(),
                BonusMilestones = new List<BonusMilestoneUIData>(),
                TotalBonusExpEarned = _model.GetBonusExp()
            };

            foreach (var item in _database.PassItems)
            {
                viewData.Milestones.Add(new MilestoneUIData
                {
                    Index = item.index,
                    RequiredExp = item.expRequired,
                    FreeRewards = item.freePassrewards, // Tham chiếu trực tiếp, an toàn
                    PremiumRewards = item.premiumPassrewards,
                    FreeState = _model.GetMilestoneState(item.index, false),
                    PremiumState = _model.GetMilestoneState(item.index, true)
                });
            }

            // Tối ưu: Duyệt qua danh sách đã được cache thay vì gọi OrderBy() liên tục
            foreach (var bonusItem in _sortedBonusItemsCache)
            {
                viewData.BonusMilestones.Add(new BonusMilestoneUIData
                {
                    Index = bonusItem.index,
                    RequiredExp = bonusItem.expRequied,
                    Rewards = bonusItem.bonusPassrewards, // Tham chiếu trực tiếp, an toàn
                    State = _model.GetBonusMilestoneState(bonusItem.index)
                });
            }

            return viewData;
        }

        private void HandleClaimReward(int index, bool isPremium)
        {
            _model.ClaimReward(index, isPremium);
        }

        private void HandleClaimBonus(int index)
        {
            _model.ClaimBonusReward(index);
        }

        private void HandleBuyPremium()
        {
            _model.UnlockPremium();
        }

        private void CleanUpDestroyedViews()
        {
            _activeViews.RemoveAll(view => view == null || (view is UnityEngine.MonoBehaviour mb && mb == null));
        }

        public void Cleanup()
        {
            _model.OnDataChanged -= HandleModelDataChanged;
            _activeViews.Clear();
        }
    }
}