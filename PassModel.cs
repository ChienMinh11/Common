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
        public event Action<List<PassRewardData>> OnRewardsClaimed;

        public int CurrentExp => _saveData.currentExp;
        public bool IsPremiumUnlocked => _saveData.isPremiumUnlocked;
        public string EventId => _eventScheduler.eventId;
        public List<PassRewardData> AutoClaimedRewards { get; private set; } = new List<PassRewardData>();
        private readonly List<IPassRewardModifier> _rewardModifiers = new List<IPassRewardModifier>();

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
        
        public void RegisterModifier(IPassRewardModifier modifier)
        {
            if (!_rewardModifiers.Contains(modifier))
            {
                _rewardModifiers.Add(modifier);
                OnDataChanged?.Invoke(); 
            }
        }

        public void UnregisterModifier(IPassRewardModifier modifier)
        {
            if (_rewardModifiers.Remove(modifier))
            {
                OnDataChanged?.Invoke();
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
                        // SỬA Ở ĐÂY: Lấy qua bộ lọc
                        rewards.AddRange(GetFinalRewards(item.index, false, false, item.freePassrewards));
                    }
                    if (oldData.isPremiumUnlocked && !oldData.claimedPremiumMilestones.Contains(item.index))
                    {
                        // SỬA Ở ĐÂY: Lấy qua bộ lọc
                        rewards.AddRange(GetFinalRewards(item.index, true, false, item.premiumPassrewards));
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
                    rewards.AddRange(GetFinalRewards(bonusItem.index, false, true, bonusItem.bonusPassrewards));
                    oldClaimedBonus.Add(bonusItem.index);
                }
            }

            return rewards;
        }
        public List<PassRewardData> GetFinalRewards(int index, bool isPremium, bool isBonus, List<PassRewardData> originalRewards)
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
 
            if (_bonusItemsCache.TryGetValue(index, out var bonusItem))
            {
                var finalRewards = GetFinalRewards(index, false, true, bonusItem.bonusPassrewards);
                OnRewardsClaimed?.Invoke(finalRewards);
            }

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
            
            // XỬ LÝ TẠI ĐÂY: Lấy quà gốc
            var originalRewards = isPremium ? _database.PassItems.FirstOrDefault(i => i.index == index)?.premiumPassrewards 
                : _database.PassItems.FirstOrDefault(i => i.index == index)?.freePassrewards;

            if (originalRewards != null)
            {
                // Chạy qua hàm GetFinalRewards đã làm ở bước trước để lấy quà sau khi replace
                var finalRewards = GetFinalRewards(index, isPremium, false, originalRewards);
                
                // Bắn event ra bên ngoài xử lý add quà
                OnRewardsClaimed?.Invoke(finalRewards);
            }
          
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
            return true;
        }
        public DateTime EventEndTime => _eventScheduler.endTime;

        public void Cleanup()
        {
            OnDataChanged = null;
        }
    }
}