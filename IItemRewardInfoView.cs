using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IItemRewardInfoView
    {
        void ShowRewards(IEnumerable<IItemReward> rewards, Transform anchor = null, string title = "");
        void Hide();
    }
}
