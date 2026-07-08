using ChieChie.Constracts;
using UnityEngine;

namespace ChieChie.GamePass
{
    [CreateAssetMenu(fileName = "NewPassBonusBankItem", menuName = "CORE/GamePass/Pass Bonus Bank Data")]
    public class PassBonusBankData : ScriptableObject
    {
        [Header("Identity Reference")]
        [Tooltip("Kéo thả ScriptableObject (có implement IPassIdentitySource) vào đây")]
        [SerializeField]
        private UnityEngine.Object identitySource;

        public UnityEngine.Object IdentitySource => identitySource;
        public IPassIdentitySource Identity => identitySource as IPassIdentitySource;
        
        public int maxRewardAmount;
        public int expConvertToAmount;
        public GameObject bonusBankIcon;
        public bool UseCustomIcon => bonusBankIcon!= null;


        public string ResourceId
        {
            get
            {
                if (Identity != null && !string.IsNullOrEmpty(Identity.ResourceId))
                {
                    return Identity.ResourceId;
                }

                return string.Empty;
            }
        }
    }
}
