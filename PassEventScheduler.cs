using System;
using ChieChie.Constracts;

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
