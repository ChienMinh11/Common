using System;
using System.Collections.Generic;
using System.Linq;

namespace ChieChie.Constracts
{
    public interface IEventViewDisplayState
    {
        string ViewId { get; set; }
        int DisplayedPoints { get; set; }
    }

    public interface IEventProgressSaveData<TViewDisplayState>
        where TViewDisplayState : class, IEventViewDisplayState
    {
        int CurrentPoints { get; set; }
        bool HasDelayedUIUpdate { get; set; }
        int DelayedDisplayPoints { get; set; }
        List<TViewDisplayState> ViewDisplayStates { get; set; }
    }

    public class EventProgressTracker<TSaveData, TViewDisplayState>
        where TSaveData : class, IEventProgressSaveData<TViewDisplayState>
        where TViewDisplayState : class, IEventViewDisplayState, new()
    {
        public void EnsureDefaults(TSaveData saveData)
        {
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));

            if (saveData.ViewDisplayStates == null)
            {
                saveData.ViewDisplayStates = new List<TViewDisplayState>();
                return;
            }

            saveData.ViewDisplayStates.RemoveAll(state => state == null);
        }

        public void AddPoints(TSaveData saveData, int amount, bool delayUpdateUI)
        {
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));

            if (delayUpdateUI)
            {
                BeginDelayedUIUpdate(saveData);
            }
            else
            {
                ClearDelayedUIUpdate(saveData);
            }

            saveData.CurrentPoints += amount;
        }

        public int GetDisplayedPoints(TSaveData saveData, string viewId)
        {
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));

            if (!saveData.HasDelayedUIUpdate)
            {
                return saveData.CurrentPoints;
            }

            var viewDisplayState = GetViewDisplayState(saveData, viewId);
            return viewDisplayState != null ? viewDisplayState.DisplayedPoints : saveData.DelayedDisplayPoints;
        }

        public bool FlushDelayedUIUpdate(TSaveData saveData, string viewId)
        {
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));
            if (!saveData.HasDelayedUIUpdate || string.IsNullOrEmpty(viewId)) return false;

            SetViewDisplayedPoints(saveData, viewId, saveData.CurrentPoints);
            return true;
        }

        public bool FlushDelayedUIUpdate(TSaveData saveData)
        {
            if (saveData == null) throw new ArgumentNullException(nameof(saveData));
            if (!saveData.HasDelayedUIUpdate) return false;

            ClearDelayedUIUpdate(saveData);
            return true;
        }

        private void BeginDelayedUIUpdate(TSaveData saveData)
        {
            if (saveData.HasDelayedUIUpdate) return;

            saveData.HasDelayedUIUpdate = true;
            saveData.DelayedDisplayPoints = saveData.CurrentPoints;
            EnsureDefaults(saveData);
            saveData.ViewDisplayStates.Clear();
        }

        private void ClearDelayedUIUpdate(TSaveData saveData)
        {
            saveData.HasDelayedUIUpdate = false;
            saveData.DelayedDisplayPoints = saveData.CurrentPoints;
            EnsureDefaults(saveData);
            saveData.ViewDisplayStates.Clear();
        }

        private TViewDisplayState GetViewDisplayState(TSaveData saveData, string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return null;

            EnsureDefaults(saveData);
            return saveData.ViewDisplayStates.FirstOrDefault(state => state.ViewId == viewId);
        }

        private void SetViewDisplayedPoints(TSaveData saveData, string viewId, int displayedPoints)
        {
            EnsureDefaults(saveData);

            var viewDisplayState = GetViewDisplayState(saveData, viewId);
            if (viewDisplayState == null)
            {
                viewDisplayState = new TViewDisplayState { ViewId = viewId };
                saveData.ViewDisplayStates.Add(viewDisplayState);
            }

            viewDisplayState.DisplayedPoints = displayedPoints;
        }
    }
}
