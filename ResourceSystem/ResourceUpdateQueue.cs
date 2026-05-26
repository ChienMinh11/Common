using System.Collections.Generic;
using UnityEngine;

namespace MyFramework
{
    public class ResourceUpdateQueue
    {
        private Queue<ResourceUpdateData> updateQueue = new Queue<ResourceUpdateData>();
        private bool isProcessing = false;

        public void EnqueueUpdate(ResourceUpdateData updateData)
        {
            updateQueue.Enqueue(updateData);
        }

        public bool HasPendingUpdates => updateQueue.Count > 0;

        public ResourceUpdateData ProcessNextUpdate()
        {
            if (updateQueue.Count > 0 && !isProcessing)
            {
                isProcessing = true;
                var update = updateQueue.Dequeue();
                isProcessing = false;
                return update;
            }
            return null;
        }

        public void Clear()
        {
            updateQueue.Clear();
            isProcessing = false;
        }
    }

    public class ResourceUpdateData
    {
        public ResourceType ResourceId { get; }
        public long Amount { get; }
        public bool IsInfinite { get; set; }
        public float InfiniteDuration { get; set; }

        public ResourceUpdateData(ResourceType resourceId, long amount)
        {
            ResourceId = resourceId;
            Amount = amount;
            IsInfinite = false;
            InfiniteDuration = 0f;
        }
    }
}
