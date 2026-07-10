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
        private List<PassBonusData> _sortedBonusItemsCache;
        public PassPresenter(PassModel model, PassDatabase database)
        {
            _model = model;
            _database = database;
            Initialize();
        }

        private void Initialize()
        {
            _model.OnDataChanged += HandleModelDataChanged;
            _sortedBonusItemsCache = _database.BonusPassItems.OrderBy(b => b.index).ToList();
        }

        public void RegisterView(IPassView view)
        {
            if (view == null) return;

            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view))
            {
                _activeViews.Add(view);
                view.OnClaimRewardClicked += HandleClaimReward;
                view.OnClaimBonusClicked += HandleClaimBonus;
                view.OnClaimBonusBankClicked += HandleClaimBonusBank;
                view.OnBuyPremiumClicked += HandleBuyPremium;

                RefreshView(view, UpdateViewDataForView(view));
            }
        }

        public void UnregisterView(IPassView view)
        {
            if (_activeViews.Contains(view))
            {
                view.OnClaimRewardClicked -= HandleClaimReward;
                view.OnClaimBonusClicked -= HandleClaimBonus;
                view.OnClaimBonusBankClicked -= HandleClaimBonusBank;
                view.OnBuyPremiumClicked -= HandleBuyPremium;
                _activeViews.Remove(view);
            }
        }

        private void HandleModelDataChanged()
        {
            CleanUpDestroyedViews();
            if (_model.HasDelayedUIUpdate)
            {
                foreach (var view in _activeViews)
                {
                    RefreshView(view, UpdateViewDataForView(view));
                }

                return;
            }

            var freshData = UpdateViewData(_model.CurrentExp);

            foreach (var view in _activeViews)
            {
                RefreshView(view, freshData);
            }
        }

        public void ForceUpdateUI(IPassView view)
        {
            if (view == null) return;
            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view)) return;
            var freshData = UpdateViewData(_model.CurrentExp);
            RefreshView(view, freshData);
        }

        public void FlushDelayedUIUpdate(IPassView view)
        {
            if (view == null) return;

            CleanUpDestroyedViews();
            if (!_activeViews.Contains(view)) return;

            _model.FlushDelayedUIUpdate(view.ViewId);
            RefreshView(view, UpdateViewDataForView(view));
        }

        private PassViewData UpdateViewDataForView(IPassView view)
        {
            return UpdateViewData(_model.GetDisplayedExp(view.ViewId));
        }

        private PassViewData UpdateViewData(int displayedExp)
        {
            var viewData = new PassViewData
            {
                CurrentExp = displayedExp,
                IsPremiumUnlocked = _model.IsPremiumUnlocked,
                CurrentMilestoneIndex = _model.GetCurrentMilestoneIndex(displayedExp),
                EventEndTime = _model.EventEndTime,
                Milestones = new List<MilestoneUIData>(),
                BonusMilestones = new List<BonusMilestoneUIData>(),
                TotalBonusExpEarned = _model.GetBonusExp(displayedExp)
            };

            foreach (var item in _database.PassItems)
            {
                viewData.Milestones.Add(new MilestoneUIData
                {
                    Index = item.index,
                    RequiredExp = item.expRequired,
                    FreeRewards = _model.GetFinalRewards(item.index, false, false, item.FreePassrewards), 
                    PremiumRewards = _model.GetFinalRewards(item.index, true, false, item.PremiumPassrewards),
                    FreeState = _model.GetMilestoneState(item.index, false, displayedExp),
                    PremiumState = _model.GetMilestoneState(item.index, true, displayedExp),
                    CustomIconFreePass = item.customIconFreePass,
                    CustomIconPremiumPass = item.customIconPremiumPass
                });
            }

            foreach (var bonusItem in _sortedBonusItemsCache)
            {
                viewData.BonusMilestones.Add(new BonusMilestoneUIData
                {
                    Index = bonusItem.index,
                    RequiredExp = bonusItem.expRequied,
                    Rewards = _model.GetFinalRewards(bonusItem.index, false, true, bonusItem.BonusPassrewards), 
                    State = _model.GetBonusMilestoneState(bonusItem.index, displayedExp),
                    BonusIcon = bonusItem.bonusIcon
                });
            }

            if (_model.HasBonusBank)
            {
                var bonusBankData = _database.BonusBankData;
                viewData.BonusBank = new BonusBankUIData
                {
                    CurrentAmount = _model.GetBonusBankAmount(displayedExp),
                    MaxAmount = bonusBankData.maxRewardAmount,
                    ExpConvertToAmount = bonusBankData.expConvertToAmount,
                    RequiredExpToMax = _model.GetBonusBankRequiredExpToMax(),
                    IsUnlocked = _model.IsNormalPassCompleted(displayedExp),
                    State = _model.GetBonusBankState(displayedExp),
                    BonusBankIcon = bonusBankData.bonusBankIcon
                };
            }

            return viewData;
        }

        private void RefreshView(IPassView view, PassViewData viewData)
        {
            if (view == null || viewData == null) return;

            view.RefreshUI(viewData);
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

        private void CleanUpDestroyedViews()
        {
            for (int i = _activeViews.Count - 1; i >= 0; i--)
            {
                var view = _activeViews[i];
                if (view == null || (view is UnityEngine.MonoBehaviour mb && mb == null))
                {
                    _activeViews.RemoveAt(i);
                }
            }
        }

        public void Cleanup()
        {
            _model.OnDataChanged -= HandleModelDataChanged;
            _activeViews.Clear();
        }
    }
}
