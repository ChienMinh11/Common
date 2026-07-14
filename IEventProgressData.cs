using System.Collections.Generic;

namespace ChieChie.Constracts
{
    public interface IEventProgressData
    {
        IReadOnlyList<int> GetNormalMilestoneRequiredPoints();
        IReadOnlyList<int> GetBonusMilestoneRequiredPoints();
        int TotalBonusPointsEarned { get; }
    }
}
