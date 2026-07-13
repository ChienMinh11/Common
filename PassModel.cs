using System;
using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;
using ChieChie.GenericLiveOps;

namespace ChieChie.GamePass
{
    public class PassModel
    {
        private readonly PassDatabase _database;
        private readonly EventSaveController<PassSaveData> _saveController;
        private readonly EventProgressTracker<PassSaveData, PassViewDisplayState> _progressTracker = new EventProgressTracker<PassSaveData, PassViewDisplayState>();
        private readonly IEventScheduler _eventScheduler;
        private PassSaveData _saveData;
        private int _totalRequiredNormalExp;
        private Dictionary<int, PassBonusData> _bonusItemsCache;
 
        public event Action OnDataChanged;
        public event Action<List<IItemReward>, PassRewardSource> OnRewardsClaimed;

        public int CurrentExp => _saveData.currentExp;
        public bool HasDelayedUIUpdate => _saveData != null && _saveData.hasDelayedUIUpdate;
        public bool IsPremiumUnlocked => _saveData.isPremiumUnlocked;
        public bool HasBonusBank => IsBonusBankConfigured();
        public string EventId => _saveData.currentEventId; // Lấy theo ID đang chạy trong SaveData thay vì scheduler
        public List<IItemReward> AutoClaimedRewards { get; private set; } = new List<IItemReward>();
        private readonly List<IPassRewardModifier> _rewardModifiers = new List<IPassRewardModifier>();
        
        public List<IItemReward> AutoClaimedNormalRewards { get; private set; } = new List<IItemReward>();
        public List<IItemReward> AutoClaimedBonusRewards { get; private set; } = new List<IItemReward>();
        public List<IItemReward> AutoClaimedBonusBankRewards { get; private set; } = new();
        public event Action<IPassNotificationEventData> OnAutoClaimNotificationTriggered;
        public event Action<IPassNotificationEventData> OnBonusBankClaimNotificationTriggered; 

        private readonly ITimeProvider _timeProvider;
        public bool IsEventActive => _eventScheduler != null && _eventScheduler.IsActive;
        public bool IsFirstOpen => _saveData != null && _saveData.isFirstOpen;

        public PassModel(PassDatabase database, IPassSaveAdapter passSaveAdapter, IEventScheduler eventScheduler, ITimeProvider timeProvider)
        {
            _database = database;
            _saveController = new EventSaveController<PassSaveData>(passSaveAdapter, EnsureSaveDataDefaults);
            _eventScheduler = eventScheduler;
            _timeProvider = timeProvider;
            CacheStaticDatabaseData();
            Initialize();
        }

        public void Initialize()
        {
            _saveData = _saveController.LoadOrCreate();
            ClearAutoClaimedRewardCaches();
            if (!string.IsNullOrEmpty(_saveData.currentEventId))
            {
                if (_eventScheduler.TryGetEventWindow(_saveData.currentEventId, out _, out var savedEndTime))
                {
                    if (_timeProvider.UtcNow > savedEndTime)
                    {
                        
                        ProcessAutoClaimUnclaimedRewards(_saveData);
                        _saveData = _saveController.Replace(new PassSaveData { currentEventId = string.Empty });
                    }
                }
            }
          
            SyncSchedulerWithSaveData();
            NotifyDataChanged();
        }

        private void EnsureSaveDataDefaults(PassSaveData saveData)
        {
            if (saveData == null) return;

            _progressTracker.EnsureDefaults(saveData);

            if (saveData.claimedFreeMilestones == null)
            {
                saveData.claimedFreeMilestones = new List<int>();
            }

            if (saveData.claimedPremiumMilestones == null)
            {
                saveData.claimedPremiumMilestones = new List<int>();
            }

            if (saveData.claimedBonusMilestones == null)
            {
                saveData.claimedBonusMilestones = new List<int>();
            }
        }
      
        public void ActivateNewEventManual()
        {
            _eventScheduler.UpdateSchedule(_timeProvider);
            if (_saveData.currentEventId != _eventScheduler.EventId)
            {
                _saveData = _saveController.Replace(new PassSaveData { currentEventId = _eventScheduler.EventId });
            }
            NotifyDataChanged();
        }
    
        private void SyncSchedulerWithSaveData()
        {
            if (string.IsNullOrEmpty(_saveData.currentEventId))
            {
                _eventScheduler.Clear();
            }
            else
            {
                _eventScheduler.SyncFromEventId(_saveData.currentEventId, _timeProvider);
            }
        }
        public void MarkFirstOpenCompleted()
        {
            if (_saveData == null || _saveData.isFirstOpen) return;
        
            _saveData.isFirstOpen = true;
            _saveController.Save();
        }

        private void CacheStaticDatabaseData()
        {
            _totalRequiredNormalExp = 0;
            foreach (var item in _database.PassItems)
            {
                _totalRequiredNormalExp += item.expRequired;
            }

            _bonusItemsCache = new Dictionary<int, PassBonusData>();
            foreach (var bonusItem in _database.BonusPassItems)
            {
                if (!_bonusItemsCache.ContainsKey(bonusItem.index))
                {
                    _bonusItemsCache.Add(bonusItem.index, bonusItem);
                }
            }
        }
        
        public void RegisterModifier(IPassRewardModifier modifier)
        {
            if (!_rewardModifiers.Contains(modifier))
            {
                _rewardModifiers.Add(modifier);
                NotifyDataChanged();
            }
        }

        public void UnregisterModifier(IPassRewardModifier modifier)
        {
            if (_rewardModifiers.Remove(modifier))
            {
                NotifyDataChanged();
            }
        }
        
        public void ClearAutoClaimedRewards()
        {
            ClearAutoClaimedRewardCaches();
        }

        private void ClearAutoClaimedRewardCaches()
        {
            AutoClaimedNormalRewards.Clear();
            AutoClaimedBonusRewards.Clear();
            AutoClaimedBonusBankRewards.Clear();
            AutoClaimedRewards.Clear();
        }

        private void ProcessAutoClaimUnclaimedRewards(PassSaveData oldData)
        {
            ClearAutoClaimedRewardCaches();

            if (oldData == null) return;
            int tempExp = oldData.currentExp;
            int oldMaxMilestoneIndex = 0;
            
            var sortedPassItems = _database.PassItems.OrderBy(i => i.index).ToList();
            
            foreach (var item in sortedPassItems)
            {
                if (tempExp >= item.expRequired)
                {
                    oldMaxMilestoneIndex = item.index;
                    tempExp -= item.expRequired;
                }
                else break;
            }
          
            foreach (var item in sortedPassItems)
            {
                if (oldMaxMilestoneIndex >= item.index)
                {
                    if (!oldData.claimedFreeMilestones.Contains(item.index))
                    {
                        var rewards = GetFinalRewards(item.index, false, false, item.FreePassrewards);
                        AutoClaimedNormalRewards.AddRange(rewards);
                        AutoClaimedRewards.AddRange(rewards);
                    }
                    if (oldData.isPremiumUnlocked && !oldData.claimedPremiumMilestones.Contains(item.index))
                    {
                        var rewards = GetFinalRewards(item.index, true, false, item.PremiumPassrewards);
                        AutoClaimedNormalRewards.AddRange(rewards);
                        AutoClaimedRewards.AddRange(rewards);
                    }
                }
            }

            if (oldData.isPremiumUnlocked) 
            {
                int oldBonusExp = Math.Max(0, oldData.currentExp - _totalRequiredNormalExp);
                var oldClaimedBonus = new HashSet<int>(oldData.claimedBonusMilestones);
                var sortedBonusItems = _database.BonusPassItems.OrderBy(b => b.index).ToList();

                foreach (var bonusItem in sortedBonusItems)
                {
                    if (oldClaimedBonus.Contains(bonusItem.index)) continue;

                    bool hasEnoughExp = oldBonusExp >= bonusItem.expRequied;
                    bool isPreviousClaimed = bonusItem.index == 0 || oldClaimedBonus.Contains(bonusItem.index - 1);

                    if (hasEnoughExp && isPreviousClaimed)
                    {
                        var rewards = GetFinalRewards(bonusItem.index, false, true, bonusItem.BonusPassrewards);
                        AutoClaimedBonusRewards.AddRange(rewards);
                        AutoClaimedRewards.AddRange(rewards);
                        oldClaimedBonus.Add(bonusItem.index);
                    }
                }
            }

            if (!oldData.isBonusBankClaimed && GetBonusBankAmount(oldData.currentExp) >= GetBonusBankMaxAmount())
            {
                var bonusBankReward = CreateBonusBankReward(GetBonusBankMaxAmount());
                if (bonusBankReward != null)
                {
                    AutoClaimedBonusBankRewards.Add(bonusBankReward);
                    AutoClaimedRewards.Add(bonusBankReward);
                }
            }
        }

        public List<IItemReward> GetFinalRewards(int index, bool isPremium, bool isBonus, List<IItemReward> originalRewards)
        {
            var finalRewards = originalRewards;
            foreach (var modifier in _rewardModifiers)
            {
                if (modifier.ShouldReplaceReward(index, isPremium, isBonus))
                {
                    finalRewards = modifier.GetReplacedRewards(index, isPremium, isBonus, finalRewards);
                }
            }
            return finalRewards;
        }
        
        public void TriggerAutoClaimNotifications()
        {
            //// Phát theo thứ tự thì việc show popup mới đúng flow
            if (AutoClaimedBonusBankRewards.Count > 0)
            {
                OnAutoClaimNotificationTriggered?.Invoke(
                    new PassNotificationEventData(new List<IItemReward>(AutoClaimedBonusBankRewards), false, true));
            }
            if (AutoClaimedBonusRewards.Count > 0)
            {
                OnAutoClaimNotificationTriggered?.Invoke(new PassNotificationEventData(new List<IItemReward>(AutoClaimedBonusRewards), true,false));
            }
            if (AutoClaimedNormalRewards.Count > 0)
            {
                OnAutoClaimNotificationTriggered?.Invoke(new PassNotificationEventData(new List<IItemReward>(AutoClaimedNormalRewards), false,false));
            }
           
        }

        public bool AddExp(int amount)
        {
            return AddExp(amount, false);
        }

        public bool AddExp(int amount, bool delayUpdateUI)
        {
            if (!_eventScheduler.IsActive) return false;

            _progressTracker.AddPoints(_saveData, amount, delayUpdateUI);
            _saveController.Save();
            if (delayUpdateUI)
            {
                return true;
            }

            NotifyDataChanged();
            return true;
        }

        public int GetDisplayedExp(string viewId)
        {
            return _progressTracker.GetDisplayedPoints(_saveData, viewId);
        }

        public void FlushDelayedUIUpdate(string viewId)
        {
            if (!_progressTracker.FlushDelayedUIUpdate(_saveData, viewId)) return;

            _saveController.Save();
        }

        public void FlushDelayedUIUpdate()
        {
            if (!_progressTracker.FlushDelayedUIUpdate(_saveData)) return;

            _saveController.Save();
        }

        public void UnlockPremium()
        {
            if (_saveData.isPremiumUnlocked) return;
            _saveData.isPremiumUnlocked = true;
          
            if (!_saveData.claimedPremiumMilestones.Contains(0))
            {
                _saveData.claimedPremiumMilestones.Add(0);
            }

            _saveController.Save();
            NotifyDataChanged();
        }

        public int GetCurrentMilestoneIndex()
        {
            return GetCurrentMilestoneIndex(_saveData.currentExp);
        }

        public int GetCurrentMilestoneIndex(int exp)
        {
            int currentMilestone = 0;
          
            var sortedPassItems = _database.PassItems.OrderBy(i => i.index).ToList();
    
            foreach (var item in sortedPassItems)
            {
                if (exp >= item.expRequired)
                {
                    currentMilestone = item.index;
                    exp -= item.expRequired;
                }
                else break;
            }
            return currentMilestone;
        }

        public int GetBonusExp()
        {
            return GetBonusExp(_saveData.currentExp);
        }

        public int GetBonusExp(int exp)
        {
            int bonusExp = exp - _totalRequiredNormalExp;
            return Math.Max(0, bonusExp);
        }

        public bool IsNormalPassCompleted(int exp)
        {
            return exp >= _totalRequiredNormalExp;
        }

        public int GetBonusBankAmount()
        {
            return GetBonusBankAmount(_saveData.currentExp);
        }

        public int GetBonusBankAmount(int exp)
        {
            if (!IsBonusBankConfigured()) return 0;

            int bonusExp = GetBonusExp(exp);
            long amount = (long)bonusExp * _database.BonusBankData.expConvertToAmount;
            return amount >= _database.BonusBankData.maxRewardAmount
                ? _database.BonusBankData.maxRewardAmount
                : (int)amount;
        }

        public int GetBonusBankRequiredExpToMax()
        {
            if (!IsBonusBankConfigured()) return 0;

            long requiredExp = (_database.BonusBankData.maxRewardAmount + (long)_database.BonusBankData.expConvertToAmount - 1) /
                               _database.BonusBankData.expConvertToAmount;
            return requiredExp > int.MaxValue ? int.MaxValue : (int)requiredExp;
        }

        public int GetBonusBankMaxAmount()
        {
            return IsBonusBankConfigured() ? _database.BonusBankData.maxRewardAmount : 0;
        }

        public MilestoneState GetBonusBankState()
        {
            return GetBonusBankState(_saveData.currentExp);
        }

        public MilestoneState GetBonusBankState(int exp)
        {
            if (!IsBonusBankConfigured()) return MilestoneState.Locked;
            if (_saveData.isBonusBankClaimed) return MilestoneState.Claimed;

            return GetBonusBankAmount(exp) >= _database.BonusBankData.maxRewardAmount
                ? MilestoneState.ReadyToClaim
                : MilestoneState.Locked;
        }

        public List<IItemReward> GetBonusBankRewards()
        {
            return GetBonusBankRewards(_saveData.currentExp);
        }

        public List<IItemReward> GetBonusBankRewards(int exp)
        {
            var reward = CreateBonusBankReward(GetBonusBankAmount(exp));
            return reward == null ? new List<IItemReward>() : new List<IItemReward> { reward };
        }

        public bool ClaimBonusBankReward()
        {
            if (GetBonusBankState() != MilestoneState.ReadyToClaim)
                return false;

            int claimAmount = GetBonusBankMaxAmount();
            var reward = CreateBonusBankReward(claimAmount);
            if (reward == null) return false;

            _saveData.isBonusBankClaimed = true;
            OnRewardsClaimed?.Invoke(new List<IItemReward> { reward }, PassRewardSource.BonusBank);

            _saveController.Save();
            NotifyDataChanged();
            OnBonusBankClaimNotificationTriggered?.Invoke(
                new PassNotificationEventData(new List<IItemReward> { reward }, false, true));
            return true;
        }

        private bool IsBonusBankConfigured()
        {
            var bonusBankData = _database != null ? _database.BonusBankData : null;
            return bonusBankData != null &&
                   bonusBankData.maxRewardAmount > 0 &&
                   bonusBankData.expConvertToAmount > 0 &&
                   !string.IsNullOrEmpty(bonusBankData.ResourceId);
        }

        private IItemReward CreateBonusBankReward(int amount)
        {
            if (!IsBonusBankConfigured() || amount <= 0) return null;
            return new BonusBankReward(_database.BonusBankData, amount);
        }

        public MilestoneState GetBonusMilestoneState(int index)
        {
            return GetBonusMilestoneState(index, _saveData.currentExp);
        }

        public MilestoneState GetBonusMilestoneState(int index, int exp)
        {
            if (!_saveData.isPremiumUnlocked) 
                return MilestoneState.Locked;
            
            if (_saveData.claimedBonusMilestones.Contains(index))
                return MilestoneState.Claimed;

            if (!_bonusItemsCache.TryGetValue(index, out var bonusItem)) 
                return MilestoneState.Locked;

            int bonusExp = GetBonusExp(exp);
            bool hasEnoughExp = bonusExp >= bonusItem.expRequied;
            bool isPreviousClaimed = index == 0 || _saveData.claimedBonusMilestones.Contains(index - 1);

            if (hasEnoughExp && isPreviousClaimed)
            {
                return MilestoneState.ReadyToClaim;
            }

            return MilestoneState.Locked;
        }

        public bool ClaimBonusReward(int index)
        {
            if (GetBonusMilestoneState(index) != MilestoneState.ReadyToClaim)
                return false;

            _saveData.claimedBonusMilestones.Add(index);
 
            if (_bonusItemsCache.TryGetValue(index, out var bonusItem))
            {
                var finalRewards = GetFinalRewards(index, false, true, bonusItem.BonusPassrewards);
        
                
                OnRewardsClaimed?.Invoke(finalRewards, PassRewardSource.Bonus);
            }

            _saveController.Save();
            NotifyDataChanged();
            return true;
        }
        
        public MilestoneState GetMilestoneState(int index, bool isPremium)
        {
            return GetMilestoneState(index, isPremium, _saveData.currentExp);
        }

        public MilestoneState GetMilestoneState(int index, bool isPremium, int exp)
        {
            int currentMilestone = GetCurrentMilestoneIndex(exp);
            if (isPremium && !_saveData.isPremiumUnlocked) return MilestoneState.Locked;

            var claimedList = isPremium ? _saveData.claimedPremiumMilestones : _saveData.claimedFreeMilestones;
            if (claimedList.Contains(index)) return MilestoneState.Claimed;
            if (currentMilestone >= index) return MilestoneState.ReadyToClaim;

            return MilestoneState.Locked;
        }

        public bool ClaimReward(int index, bool isPremium)
        {
            if (GetMilestoneState(index, isPremium) != MilestoneState.ReadyToClaim) return false;
    
            if (isPremium) _saveData.claimedPremiumMilestones.Add(index);
            else _saveData.claimedFreeMilestones.Add(index);
    
            var originalRewards = isPremium ? _database.PassItems.FirstOrDefault(i => i.index == index)?.PremiumPassrewards 
                : _database.PassItems.FirstOrDefault(i => i.index == index)?.FreePassrewards;

            if (originalRewards != null)
            {
                var finalRewards = GetFinalRewards(index, isPremium, false, originalRewards);
        
                var source = isPremium ? PassRewardSource.Premium : PassRewardSource.Normal;
                OnRewardsClaimed?.Invoke(finalRewards, source);
            }
  
            _saveController.Save();
            NotifyDataChanged();
            return true;
        }
        public DateTime EventEndTime => _eventScheduler.EndTime;

        public void RefreshData()
        {
            NotifyDataChanged();
        }

        private void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }

        public void Cleanup()
        {
            OnDataChanged = null;
        }

        private class BonusBankReward : IItemReward
        {
            private readonly PassBonusBankData _bonusBankData;

            public BonusBankReward(PassBonusBankData bonusBankData, long amount)
            {
                _bonusBankData = bonusBankData;
                Amount = amount;
            }

            public string ResourceId => _bonusBankData.ResourceId;
            public long Amount { get; }
            public bool IsInfiniteReward => false;
            public float InfinityDuration => 0f;
            public UnityEngine.Sprite IconReward => _bonusBankData.Identity != null ? _bonusBankData.Identity.Icon : null;
            public UnityEngine.Sprite InfinityRewardIcon => _bonusBankData.Identity != null ? _bonusBankData.Identity.InfinityIcon : null;
        }
    }
}
