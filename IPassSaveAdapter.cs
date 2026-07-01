using UnityEngine;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    public class PassSaveData
    {
        public string currentEventId;
        public int currentExp;
        public bool isPremiumUnlocked;
        public List<int> claimedFreeMilestones = new List<int>();
        public List<int> claimedPremiumMilestones = new List<int>();
        public List<int> claimedBonusMilestones = new List<int>();
    }

    public interface IPassSaveAdapter
    {
        PassSaveData LoadData();
        void SaveData(PassSaveData data);
    }
}
