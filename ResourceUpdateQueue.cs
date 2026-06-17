using System.Collections.Generic;

namespace ChieChie.Resource
{
    public class ResourceUpdateQueue
    {
        private readonly Queue<long> updateQueue = new Queue<long>();
        private bool isProcessing = false;

        public bool HasPendingUpdates => updateQueue.Count > 0;
      
        public void EnqueueUpdate(long oldAmount, long newAmount)
        {
            updateQueue.Enqueue(newAmount);
        }

        public bool TryDequeue(out long nextAmount)
        {
            if (updateQueue.Count > 0 && !isProcessing)
            {
                isProcessing = true;
                nextAmount = updateQueue.Dequeue();
                isProcessing = false;
                return true;
            }

            nextAmount = 0;
            return false;
        }
      
        // SỬA: Chuyển đổi ResourceType sang int resourceHash
        public ResourceUpdateData ProcessNextUpdate(int resourceHash)
        {
            if (updateQueue.Count > 0 && !isProcessing)
            {
                isProcessing = true;
                long nextAmount = updateQueue.Dequeue();
                isProcessing = false;
                return new ResourceUpdateData(resourceHash, nextAmount);
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
        public int ResourceHash { get; }
        public long Amount { get; }

        public ResourceUpdateData(int resourceHash, long amount)
        {
            ResourceHash = resourceHash;
            Amount = amount;
        }
    }
}