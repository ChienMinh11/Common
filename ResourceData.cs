using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChieChie.Resource
{
    [Serializable]
    public class ResourceData
    {
        [Header("Identity Reference")]
        [Tooltip("Kéo thả ScriptableObject (có implement IResourceIdentitySource) vào đây")]
        [SerializeField] private UnityEngine.Object identitySource; 
        public UnityEngine.Object IdentitySource => identitySource;
        public IResourceIdentitySource Identity => identitySource as IResourceIdentitySource;
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
        
        private int? _hashId;
     
        public int HashId
        {
            get
            {
                if (!_hashId.HasValue)
                {
                    string id = ResourceId;
                    if (!string.IsNullOrEmpty(id))
                    {
                        _hashId = Animator.StringToHash(id);
                    }
                    else
                    {
                        _hashId = Animator.StringToHash("INVALID_RESOURCE_ID");
                    }
                }
                return _hashId.Value;
            }
        }
        public string DisplayName => Identity.DisplayName;
       
        public Sprite Icon => Identity.Icon;
        public Sprite InfinityIcon =>Identity.InfinityIcon;
        
        [SerializeField] private long maxStack;
        [SerializeField] private long defaultAmount = 0;

        [Header("Regeneration Settings")]
        [SerializeField] private bool hasRegen = false;
        [SerializeField] private long regenAmount = 1;        
        [SerializeField] private float intervalSeconds = 1800f;   
        [SerializeField] private bool isEnabledByDefault = true;

        
#if UNITY_EDITOR
        private long runtimeMaxStack;
        private bool isRuntimeMaxStackSet;
#endif

        public long MaxStack 
        { 
            get 
            {
#if UNITY_EDITOR
                return isRuntimeMaxStackSet ? runtimeMaxStack : maxStack;
#else
                return maxStack;
#endif
            }
            set
            {
#if UNITY_EDITOR
                runtimeMaxStack = value < 0 ? 0 : value;
                isRuntimeMaxStackSet = true;
#else
                maxStack = value < 0 ? 0 : value;
#endif
            }
        }
        public long DefaultAmount => defaultAmount;
        public bool HasRegen => hasRegen;
        public long RegenAmount { get => regenAmount; set => regenAmount = value; }
        public float IntervalSeconds => intervalSeconds;
        public bool IsEnabledByDefault => isEnabledByDefault;
    }

    public readonly struct ResourceChangeData<T>
    {
        public readonly int ResourceId;
        public readonly T OldAmount;
        public readonly T NewAmount;
        public readonly bool DelayUpdate;

        public ResourceChangeData(int resourceId, T oldAmount, T newAmount, bool delayUpdate = false)
        {
            ResourceId = resourceId;
            OldAmount = oldAmount;
            NewAmount = newAmount;
            DelayUpdate = delayUpdate;
        }
    }
    
 
}