using System;
using UnityEngine;

namespace ChieChie.GamePass
{
    [Serializable]
    public class PassRewardData
    {
        [Header("Identity Reference")]
        [Tooltip("Kéo thả ScriptableObject (có implement IPassIdentitySource) vào đây")]
        [SerializeField]
        private UnityEngine.Object identitySource;

        public UnityEngine.Object IdentitySource => identitySource;
        public IPassIdentitySource Identity => identitySource as IPassIdentitySource;

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



        [SerializeField] Sprite chestIcon;
        [SerializeField] private long _amount;
        [SerializeField] private bool _isInfiniteReward;
        [SerializeField] private float _infinityDuration;

        public long Amount => _amount;
        public bool IsInfiniteReward => _isInfiniteReward;
        public float InfinityDuration => _infinityDuration;

        public bool UseChestIcon => chestIcon != null;

        public Sprite IconReward
        {
            get
            {
                if (Identity != null && Identity.Icon != null)
                {
                    return Identity.Icon;
                }

                return null;
            }
        }


        public Sprite InfinityRewardIcon
        {
            get
            {
                if (Identity != null && Identity.InfinityIcon != null)
                {
                    return Identity.InfinityIcon;
                }

                return null;
            }
        }

        public interface IPassIdentitySource
        {
            string ResourceId { get; }
            Sprite Icon { get; }
            Sprite InfinityIcon { get; }
        }
    }


}
