using UnityEngine;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    public class PassSaveData
    {
        public string currentEventId;       // ID của mùa giải hiện tại
        public int currentExp;              // Điểm kinh nghiệm hiện tại
        public bool isPremiumUnlocked;      // Đã mua Premium Pass chưa
        public List<int> claimedFreeMilestones = new List<int>();    // Mốc Free đã nhận
        public List<int> claimedPremiumMilestones = new List<int>(); // Mốc Premium đã nhận
        public int bonusClaimedCount;       // Số lần đã nhận thưởng mốc Bonus
    }

    public interface IPassSaveAdapter
    {
        PassSaveData LoadData();
        void SaveData(PassSaveData data);
    }
}
