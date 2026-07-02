using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassReplaceRewardItem", menuName = "CORE/GamePass/Pass Replace Reward")]
    public class PassReplaceItem : ScriptableObject
    {
        public int replaceIndex;
        public bool isBonus;
        public List<PassRewardData> replacePassRewards = new List<PassRewardData>();
        public List<IItemReward> ReplacePassRewards => replacePassRewards.ConvertAll(x => (IItemReward)x);
       
    }
}
