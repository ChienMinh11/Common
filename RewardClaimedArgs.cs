using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace Game.GamePlay
{
    public class RewardClaimedArgs
    {
        public List<RewardClaimedEventData> Rewards { get; set; }
        public Transform SpawnTransform { get; set; }
    }
}
