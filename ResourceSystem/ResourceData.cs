using System;
using UnityEngine;

namespace MyFramework
{
    [Serializable]
    public class ResourceData 
    {
        public ResourceType key;
        public string displayName;
        public ToastDataProvider displayNameLocalize;
        public Sprite icon;
        public Sprite infiniteIcon;
        [SerializeField] private long maxStack;
        [SerializeField] private long defaultAmount = 0;
        
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
    }
    public class ResourceChangeData<T>
    {
        public ResourceChangeData(ResourceType resourceId, T oldAmount, T newAmount)
        {
            ResourceId = resourceId;
            OldAmount = oldAmount;
            NewAmount = newAmount;
        }

        public ResourceType ResourceId { get; }
        public T OldAmount { get; }
        public T NewAmount { get; }
    }

    public class ResourceChangeDataWithDelay<T> : ResourceChangeData<T>
    {
        public ResourceChangeDataWithDelay(ResourceType resourceId, T oldAmount, T newAmount, bool delayUpdate)
            : base(resourceId, oldAmount, newAmount)
        {
            DelayUpdate = delayUpdate;
        }

        public bool DelayUpdate { get; }
    }

    public class ResourceInfiniteStatusData
    {
        public ResourceInfiniteStatusData(ResourceType resourceId, bool isInfinite, float duration, bool delayUpdate = false)
        {
            ResourceId = resourceId;
            IsInfinite = isInfinite;
            Duration = duration;
            DelayUpdate = delayUpdate;
        }

        public ResourceType ResourceId { get; }
        public bool IsInfinite { get; }
        public float Duration { get; }
        public bool DelayUpdate { get; }
    }
    
}