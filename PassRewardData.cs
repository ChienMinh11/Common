using System;
using ChieChie.Constracts;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.GamePass
{
    [Serializable]
    public class PassRewardData : IItemReward
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
        
        [SerializeField] private long amount;
        [SerializeField] private bool isInfiniteReward;
        [SerializeField] private float infinityDuration;

        public long Amount => amount;
        public bool IsInfiniteReward => isInfiniteReward;
        public float InfinityDuration => infinityDuration;

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
