using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassItem", menuName = "CORE/GamePass/Pass Data")]
    public class PassData: ScriptableObject
    {
        public int index;
        public int requiredAmount;
        public List<PassRewardData> freePassrewards = new List<PassRewardData>();
        public List<PassRewardData> PremiumPassrewards = new List<PassRewardData>();

    }

}



