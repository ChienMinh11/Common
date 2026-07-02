using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    public class RewardDisplayService
    {
        private readonly Queue<IBaseRewardDisplayData> _dataQueue = new Queue<IBaseRewardDisplayData>();
        private IBaseRewardDisplayData _currentData;

        public IBaseRewardDisplayData CurrentData 
        { 
            get
            {
                if (_currentData == null && _dataQueue.Count > 0)
                {
                    _currentData = _dataQueue.Dequeue();
                }
                return _currentData;
            } 
            private set => _currentData = value; 
        }

        public void SetContextData(IBaseRewardDisplayData data)
        {
            _currentData = data;
        }

        public void EnqueueContextData(IBaseRewardDisplayData data)
        {
            _dataQueue.Enqueue(data);
        }
    }
}
