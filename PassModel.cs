using System;
using System.Collections.Generic;
using System.Linq;

namespace ChieChie.GamePass
{
    public class PassModel
    {
        private readonly PassDatabase _database;
        private readonly IPassSaveAdapter _passSaveAdapter;
        private readonly PassEventScheduler _eventScheduler;
        private PassSaveData _saveData;
        private int _totalRequiredNormalExp;
        private Dictionary<int, PassBonusData> _bonusItemsCache;
 
        public event Action OnDataChanged;

        public int CurrentExp => _saveData.currentExp;
        public bool IsPremiumUnlocked => _saveData.isPremiumUnlocked;
        public string EventId => _eventScheduler.eventId;
        public List<PassRewardData> AutoClaimedRewards { get; private set; } = new List<PassRewardData>();

        public PassModel(PassDatabase database, IPassSaveAdapter passSaveAdapter, PassEventScheduler eventScheduler)
        {
            _database = database;
            _passSaveAdapter = passSaveAdapter;
            _eventScheduler = eventScheduler;
            CacheStaticDatabaseData();
            Initialize();
        }
        
        public void Initialize()
        {
            _eventScheduler.UpdateMonthlySchedule(DateTime.UtcNow);
            _saveData = _passSaveAdapter.LoadData() ?? new PassSaveData();
            if (!string.IsNullOrEmpty(_saveData.currentEventId) && _saveData.currentEventId != _eventScheduler.eventId)
            {
                AutoClaimedRewards = ProcessAutoClaimUnclaimedRewards(_saveData);
                _saveData = new PassSaveData { currentEventId = _eventScheduler.eventId };
                _passSaveAdapter.SaveData(_saveData);
            }
            else if (string.IsNullOrEmpty(_saveData.currentEventId))
            {
                _saveData.currentEventId = _eventScheduler.eventId;
                _passSaveAdapter.SaveData(_saveData);
            }
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
        private List<PassRewardData> ProcessAutoClaimUnclaimedRewards(PassSaveData oldData)
        {
            var rewards = new List<PassRewardData>();
            if (oldData == null) return rewards;
            int tempExp = oldData.currentExp;
            int oldMaxMilestoneIndex = 0;
            foreach (var item in _database.PassItems)
            {
                if (tempExp >= item.expRequired)
                {
                    oldMaxMilestoneIndex = item.index;
                    tempExp -= item.expRequired;
                }
                else break;
            }
            foreach (var item in _database.PassItems)
            {
                if (oldMaxMilestoneIndex >= item.index)
                {
                    if (!oldData.claimedFreeMilestones.Contains(item.index))
                    {
                        rewards.AddRange(item.freePassrewards);
                    }
                    if (oldData.isPremiumUnlocked && !oldData.claimedPremiumMilestones.Contains(item.index))
                    {
                        rewards.AddRange(item.premiumPassrewards);
                    }
                }
            }
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
                    rewards.AddRange(bonusItem.bonusPassrewards);
                    oldClaimedBonus.Add(bonusItem.index);
                }
            }

            return rewards;
        }

        public void AddExp(int amount)
        {
            if (!_eventScheduler.isActive) return;

            _saveData.currentExp += amount;
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
        }

        public void UnlockPremium()
        {
            if (_saveData.isPremiumUnlocked) return;
            _saveData.isPremiumUnlocked = true;
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
        }

        public int GetCurrentMilestoneIndex()
        {
            int exp = _saveData.currentExp;
            int currentMilestone = 0;
            foreach (var item in _database.PassItems)
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
            int bonusExp = _saveData.currentExp - _totalRequiredNormalExp;
            return Math.Max(0, bonusExp);
        }

        public MilestoneState GetBonusMilestoneState(int index)
        {
            if (_saveData.claimedBonusMilestones.Contains(index))
                return MilestoneState.Claimed;

            if (!_bonusItemsCache.TryGetValue(index, out var bonusItem)) 
                return MilestoneState.Locked;

            int bonusExp = GetBonusExp();
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
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
            return true;
        }
        
        public MilestoneState GetMilestoneState(int index, bool isPremium)
        {
            int currentMilestone = GetCurrentMilestoneIndex();
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
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
            return true;
        }

        public void Cleanup()
        {
            OnDataChanged = null;
        }
    }
}