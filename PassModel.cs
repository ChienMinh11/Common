using System;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    [Serializable]
    public class PassModel 
    {
        public string currentEventId;         // ID của mùa giải hiện tại
        public int currentPoints;             // Số điểm tích lũy hiện tại trong Tier
        public int currentTierIndex;          // Cấp độ Tier hiện tại (Bắt đầu từ 0)
        public bool isPremiumUnlocked;        // Đã mua Premium Pass chưa?
        
        // Danh sách lưu các Tier đã nhận thưởng
        public HashSet<int> claimedFreeTiers = new HashSet<int>();
        public HashSet<int> claimedPremiumTiers = new HashSet<int>();

        // Điểm thừa và số lần nhận thưởng của hũ Bonus Bank sau khi Max Cấp
        public int bonusPoints;
        public int claimedBonusCount;

        public void Reset(string eventId)
        {
            currentEventId = eventId;
            currentPoints = 0;
            currentTierIndex = 0;
            isPremiumUnlocked = false;
            claimedFreeTiers.Clear();
            claimedPremiumTiers.Clear();
            bonusPoints = 0;
            claimedBonusCount = 0;
        }
    }
}