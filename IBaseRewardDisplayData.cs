using System.Collections.Generic;

namespace ChieChie.Constracts
{
    public interface IBaseRewardDisplayData
    {
        List<IItemReward> GetRewards();
        string GetTitle();
        string GetDescription();
        string GetName();
        void OnRewardsClaimed();
        void OnClosePopup();
    }
}
