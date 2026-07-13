using System;
using ChieChie.Constracts;
using ChieChie.GenericLiveOps;

namespace ChieChie.GamePass
{
    [Serializable]
    public class PassEventScheduler : MonthlyEventScheduler
    {
        public PassEventScheduler() : base("GamePass")
        {
        }
    }
}
