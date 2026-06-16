using UnityEngine;

namespace ChieChie.Core
{
    public class RewardDisplayService
    {
        public IBaseRewardDisplayData CurrentData { get; private set; }

        public void SetContextData(IBaseRewardDisplayData data)
        {
            CurrentData = data;
        }
    }
}
