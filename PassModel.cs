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
        
        // --- CACHE VARIABLES ---
        private int _totalRequiredNormalExp;
        private Dictionary<int, PassBonusData> _bonusItemsCache;
        // -----------------------

        // Sự kiện thông báo khi dữ liệu Game Pass thay đổi (Ví dụ: tăng điểm, nhận quà)
        public event Action OnDataChanged;

        public int CurrentExp => _saveData.currentExp;
        public bool IsPremiumUnlocked => _saveData.isPremiumUnlocked;
        public string EventId => _eventScheduler.eventId;

        public PassModel(PassDatabase database, IPassSaveAdapter passSaveAdapter, PassEventScheduler eventScheduler)
        {
            _database = database;
            _passSaveAdapter = passSaveAdapter;
            _eventScheduler = eventScheduler;
            Initialize();
            CacheStaticDatabaseData();
        }
        
        
        
        public void Initialize()
        {
            // Cập nhật trạng thái sự kiện theo thời gian thực tế
            _eventScheduler.UpdateMonthlySchedule(DateTime.UtcNow);
            
            _saveData = _passSaveAdapter.LoadData() ?? new PassSaveData();

            // Nếu qua mùa mới, reset dữ liệu cũ
            if (_saveData.currentEventId != _eventScheduler.eventId)
            {
                _saveData = new PassSaveData { currentEventId = _eventScheduler.eventId };
                _passSaveAdapter.SaveData(_saveData);
            }
        }
        private void CacheStaticDatabaseData()
        {
            // 1. Cache tổng EXP mốc thường thay vì dùng .Sum() liên tục
            _totalRequiredNormalExp = 0;
            foreach (var item in _database.PassItems)
            {
                _totalRequiredNormalExp += item.expRequired;
            }

            // 2. Cache Dictionary cho các mốc Bonus thay vì dùng .FirstOrDefault()
            _bonusItemsCache = new Dictionary<int, PassBonusData>();
            foreach (var bonusItem in _database.BonusPassItems)
            {
                if (!_bonusItemsCache.ContainsKey(bonusItem.index))
                {
                    _bonusItemsCache.Add(bonusItem.index, bonusItem);
                }
            }
        }

        // Hàm thêm điểm kinh nghiệm (khi win level, làm quest)
        public void AddExp(int amount)
        {
            if (!_eventScheduler.isActive) return;

            _saveData.currentExp += amount;
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
        }

        // Mở khoá gói Premium (khi mua IAP)
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

        // Lấy lượng Exp dư ra sau khi đã trừ đi toàn bộ yêu cầu của các mốc Pass thông thường
        public int GetBonusExp()
        {
            int bonusExp = _saveData.currentExp - _totalRequiredNormalExp;
            return Math.Max(0, bonusExp);
        }
        // Kiểm tra trạng thái của một mốc Bonus cụ thể theo Index
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

        // Thực hiện logic nhận thưởng mốc Bonus
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