using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Resource
{
    public enum ResourceEventType
    {
        ResourceChanged,
        ResourceSpent,
        ResourceMaxStackReached,
        ResourceAdded,
        ResourceInsufficient,
    }

    public class ResourceModel<T>
    {
        private const string SAVE_KEY_PREFIX = "Resource_";
        private const string FIRST_INIT_KEY = "ResourceFirstInit";
        
        private ResourceConfig config;
        
        private readonly INumberConverter<T> converter;
        private readonly IEventService eventService;
        private readonly ISaveSystem saveSystem;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;

        private readonly Dictionary<int, T> resourceAmounts = new();
        private readonly Dictionary<int, string> cachedSaveKeys = new();
        public ResourceModel(INumberConverter<T> converter, IEventService eventService, ISaveSystem saveSystem, IReadOnlyInfiniteStatus infiniteStatus)
        {
            this.converter = converter;
            this.eventService = eventService;
            this.saveSystem = saveSystem;
            this._infiniteStatus = infiniteStatus;
        }

        public void Initialize(ResourceConfig config)
        {
            this.config = config;
   
            saveSystem.RegisterKey(FIRST_INIT_KEY);

            foreach (var resourceData in config.GetAllResources())
            {
                cachedSaveKeys[resourceData.HashId] = SAVE_KEY_PREFIX + resourceData.ResourceId;
            }

            foreach (var resourceData in config.GetAllResources())
            {
                var saveKey = GetSaveKey(resourceData.HashId, resourceData.ResourceId);
                saveSystem.RegisterKey(saveKey);
                resourceAmounts[resourceData.HashId] = LoadAmount(resourceData.HashId, resourceData.ResourceId);
            }

            if (!saveSystem.Load<bool>(FIRST_INIT_KEY, false))
            {
                foreach (var resourceData in config.GetAllResources())
                {long initialAmount = 0;
                   
                    if (resourceData.HasRegen)
                    {
                        initialAmount = resourceData.MaxStack;
                    }
                    
                    else if (resourceData.DefaultAmount > 0)
                    {
                        initialAmount = resourceData.DefaultAmount;
                    }

                    if (initialAmount > 0)
                    {
                        var convertedAmt = converter.FromLong(initialAmount);
                        resourceAmounts[resourceData.HashId] = convertedAmt;
                        SaveAmount(resourceData.HashId, resourceData.ResourceId, convertedAmt);
                    }
                }
                saveSystem.Save(FIRST_INIT_KEY, true);
            }
        }

        private string GetSaveKey(int hash, string resourceId)
        {
            if (cachedSaveKeys.TryGetValue(hash, out var key)) return key;
            string newKey = SAVE_KEY_PREFIX + resourceId;
            cachedSaveKeys[hash] = newKey;
            return newKey;
        }

        private T LoadAmount(int hash, string resourceId)
        {
            var saveKey = GetSaveKey(hash, resourceId);
            long savedLong = saveSystem.Load<long>(saveKey, 0L);
            return converter.FromLong(savedLong);
        }
        private void SaveAmount(int hash, string resourceId, T amount)
        {
            var saveKey = GetSaveKey(hash, resourceId);
            long longAmount = converter.ToLong(amount);
            saveSystem.Save<long>(saveKey, longAmount);
        }

        public T GetAmount(string resourceId)
        {
            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);
            return resourceAmounts.TryGetValue(hash, out var amount) ? amount : converter.Zero;
        }

        public void AddResource(string resourceId, T amount, bool delayUpdate = false)
        {
            if (converter.IsLessThan(amount, converter.Zero)) return;

            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);
            var currentAmount = resourceAmounts.TryGetValue(hash, out var amt) ? amt : converter.Zero;
            var newAmount = converter.Add(currentAmount, amount);

            var maxStack = GetMaxStack(hash);
            if (maxStack > 0)
            {
                long longNewAmount = converter.ToLong(newAmount);
                if (longNewAmount > maxStack)
                {
                    newAmount = converter.FromLong(maxStack);
                    eventService.Publish(ResourceEventType.ResourceMaxStackReached, this, hash);
                }
            }

            resourceAmounts[hash] = newAmount;
            SaveAmount(hash, resourceId, newAmount);

            var changeData = new ResourceChangeData<T>(hash, currentAmount, newAmount, delayUpdate);
            eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
            eventService.Publish(ResourceEventType.ResourceAdded, this, changeData);
        }

        public bool SpendResource(string resourceId, T amount)
        {
            if (converter.IsLessThan(amount, converter.Zero)) return false;

            int hash = string.IsNullOrEmpty(resourceId) ? 0 : Animator.StringToHash(resourceId);

            if (_infiniteStatus != null && _infiniteStatus.IsCurrentlyInfinite(hash))
            {
                var currentAmt = resourceAmounts.TryGetValue(hash, out var amt) ? amt : converter.Zero;
                var changeData = new ResourceChangeData<T>(hash, currentAmt, currentAmt); 
                eventService.Publish(ResourceEventType.ResourceSpent, this, changeData);
                return true; 
            }

            var currentAmount = resourceAmounts.TryGetValue(hash, out var cAmt) ? cAmt : converter.Zero;
            if (converter.IsLessThan(currentAmount, amount))
            {
                eventService.Publish(ResourceEventType.ResourceInsufficient, this, hash);
                return false;
            }

            var newAmount = converter.Subtract(currentAmount, amount);
            resourceAmounts[hash] = newAmount;
            SaveAmount(hash, resourceId, newAmount);

            var changeDataNormal = new ResourceChangeData<T>(hash, currentAmount, newAmount);
            eventService.Publish(ResourceEventType.ResourceChanged, this, changeDataNormal);
            eventService.Publish(ResourceEventType.ResourceSpent, this, changeDataNormal);
            return true;
        }

        public ResourceData GetResourceData(int hash) => config.GetResourceData(hash);
        public long GetMaxStack(int hash) => config.GetResourceData(hash)?.MaxStack ?? 0;

        public bool SetMaxStack(int hash, long newMaxStack)
        {
            var resourceData = config.GetResourceData(hash);
            if (resourceData == null) return false;

            resourceData.MaxStack = newMaxStack;
            if (newMaxStack > 0)
            {
                var currentAmount = resourceAmounts.TryGetValue(hash, out var amt) ? amt : converter.Zero;
                long longCurrentAmount = converter.ToLong(currentAmount);
                if (longCurrentAmount > newMaxStack)
                {
                    var newAmount = converter.FromLong(newMaxStack);
                    resourceAmounts[hash] = newAmount;
                    SaveAmount(hash, resourceData.ResourceId, newAmount);

                    var changeData = new ResourceChangeData<T>(hash, currentAmount, newAmount);
                    eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
                }
            }
            return true;
        }

        public void Cleanup()
        {
            resourceAmounts.Clear();
            cachedSaveKeys.Clear();
        }
    }
}