using System.Collections.Generic;

namespace ChieChie.Core
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

        // SỬA TẠI ĐÂY: Truyền thêm ResourceId từ Presenter vào để đóng gói dữ liệu trả về
        public ResourceUpdateData ProcessNextUpdate(ResourceType resourceId)
        {
            if (updateQueue.Count > 0 && !isProcessing)
            {
                isProcessing = true;
                long nextAmount = updateQueue.Dequeue();
                isProcessing = false;

                // Trả về dữ liệu hợp lệ thay vì null
                return new ResourceUpdateData(resourceId, nextAmount);
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

        public ResourceUpdateData(ResourceType resourceId, long amount)
        {
            ResourceId = resourceId;
            Amount = amount;
        }
    }
}