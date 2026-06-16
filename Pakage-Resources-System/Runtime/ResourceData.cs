using System;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    [Serializable]
    public class ResourceData 
    {
        public ResourceType key;
        public string displayName;
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
        public readonly ResourceType ResourceId;
        public readonly T OldAmount;
        public readonly T NewAmount;
        public readonly bool DelayUpdate;

        public ResourceChangeData(ResourceType resourceId, T oldAmount, T newAmount, bool delayUpdate = false)
        {
            ResourceId = resourceId;
            OldAmount = oldAmount;
            NewAmount = newAmount;
            DelayUpdate = delayUpdate;
        }
    }
 
}