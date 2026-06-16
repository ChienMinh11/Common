using System.Collections.Generic;

namespace ChieChie.Core
{
    public interface IBaseRewardDisplayData
    {
        List<BaseRewardData> GetRewards();
        string GetTitle();
        string GetDescription();
        string GetName();
        void OnRewardsClaimed();
        void OnClosePopup();
    }
}
