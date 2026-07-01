using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassItem", menuName = "CORE/GamePass/Pass Data")]
    public class PassData: ScriptableObject
    {
        public int index;
        public int expRequired;
        public GameObject customIconFreePass;
        public GameObject customIconPremiumPass;
        public List<PassRewardData> freePassrewards = new List<PassRewardData>();
        public List<PassRewardData> premiumPassrewards = new List<PassRewardData>();
       public bool UseCustomIconFreePass => customIconFreePass != null;
        public bool UseCustomIconPremiumPass => customIconPremiumPass != null;
    }

}



