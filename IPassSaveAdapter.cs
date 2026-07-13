using System.Collections.Generic;
using ChieChie.GenericLiveOps;

namespace ChieChie.GamePass
{
    [System.Serializable]
    public class PassViewDisplayState : IEventViewDisplayState
    {
        public string viewId;
        public int displayedExp;

        public string ViewId
        {
            get => viewId;
            set => viewId = value;
        }

        public int DisplayedPoints
        {
            get => displayedExp;
            set => displayedExp = value;
        }
    }

    [System.Serializable]
    public class PassSaveData : IEventProgressSaveData<PassViewDisplayState>
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
        public bool isFirstOpen;

        public int CurrentPoints
        {
            get => currentExp;
            set => currentExp = value;
        }

        public bool HasDelayedUIUpdate
        {
            get => hasDelayedUIUpdate;
            set => hasDelayedUIUpdate = value;
        }

        public int DelayedDisplayPoints
        {
            get => delayedDisplayExp;
            set => delayedDisplayExp = value;
        }

        public List<PassViewDisplayState> ViewDisplayStates
        {
            get => viewDisplayStates;
            set => viewDisplayStates = value;
        }
    }

    public interface IPassSaveAdapter : IEventSaveAdapter<PassSaveData>
    {
    }
}
