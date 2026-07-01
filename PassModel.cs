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

        // Tính toán xem người chơi đang ở cấp độ (Milestone Index) nào dựa trên Exp hiện tại
        public int GetCurrentMilestoneIndex()
        {
            int exp = _saveData.currentExp;
            int currentMilestone = 0;

            foreach (var item in _database.PassItems)
            {
                if (exp >= item.requiredAmount)
                {
                    currentMilestone = item.index;
                    exp -= item.requiredAmount; // Trừ lượng exp cần thiết của mốc đó
                }
                else
                {
                    break;
                }
            }
            return currentMilestone;
        }

        // Lấy Exp thừa để tính toán mốc Bonus
        public int GetBonusExp()
        {
            int totalRequired = _database.PassItems.Sum(item => item.requiredAmount);
            int bonusExp = _saveData.currentExp - totalRequired;
            return Math.Max(0, bonusExp);
        }

        // Kiểm tra trạng thái của một mốc cụ thể
        public MilestoneState GetMilestoneState(int index, bool isPremium)
        {
            int currentMilestone = GetCurrentMilestoneIndex();
            
            if (isPremium && !_saveData.isPremiumUnlocked)
                return MilestoneState.Locked; // Chưa mua Premium

            var claimedList = isPremium ? _saveData.claimedPremiumMilestones : _saveData.claimedFreeMilestones;
            if (claimedList.Contains(index))
                return MilestoneState.Claimed; // Đã nhận

            if (currentMilestone >= index)
                return MilestoneState.ReadyToClaim; // Đủ cấp, chờ nhận

            return MilestoneState.Locked; // Chưa đủ cấp
        }

        // Logic Nhận quà mốc thông thường
        public bool ClaimReward(int index, bool isPremium)
        {
            if (GetMilestoneState(index, isPremium) != MilestoneState.ReadyToClaim) 
                return false;

            if (isPremium) _saveData.claimedPremiumMilestones.Add(index);
            else _saveData.claimedFreeMilestones.Add(index);

            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
            return true;
        }

        // Kiểm tra số lượng quà Bonus có thể nhận (Infinite Loop giống Royal Match)
        public int GetAvailableBonusClaims()
        {
            if (_database.BonusPassItems.Count == 0) return 0;
            int bonusExp = GetBonusExp();
            int required = _database.BonusPassItems[0].requiredAmount; // Thường mốc bonus chỉ có 1 cấu hình lặp lại
            
            int totalEarnedBonus = bonusExp / required;
            return Math.Max(0, totalEarnedBonus - _saveData.bonusClaimedCount);
        }

        public bool ClaimBonusReward()
        {
            if (GetAvailableBonusClaims() <= 0) return false;

            _saveData.bonusClaimedCount++;
            _passSaveAdapter.SaveData(_saveData);
            OnDataChanged?.Invoke();
            return true;
        }

        public void Cleanup()
        {
            OnDataChanged = null;
        }
    }

    public enum MilestoneState
    {
        Locked,
        ReadyToClaim,
        Claimed
    }
}