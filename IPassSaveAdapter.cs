using UnityEngine;
using System.Collections.Generic;

namespace ChieChie.GamePass
{
    [System.Serializable]
    public class PassViewDisplayState
    {
        public string viewId;
        public int displayedExp;
    }

    [System.Serializable]
    public class PassSaveData
    {
        public string currentEventId;
        public int currentExp;
        public bool hasDelayedUIUpdate;
        public int delayedDisplayExp;
        public List<PassViewDisplayState> viewDisplayStates = new List<PassViewDisplayState>();
        public bool isPremiumUnlocked;
        public List<int> claimedFreeMilestones = new List<int>();
        public List<int> claimedPremiumMilestones = new List<int>();
        public List<int> claimedBonusMilestones = new List<int>();
        public bool isBonusBankClaimed;
    }

    public interface IPassSaveAdapter
    {
        PassSaveData LoadData();
        void SaveData(PassSaveData data);
    }
}
