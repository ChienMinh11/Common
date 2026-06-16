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

        private readonly Dictionary<ResourceType, T> resourceAmounts = new();
        private readonly Dictionary<ResourceType, string> cachedSaveKeys = new();
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
                cachedSaveKeys[resourceData.key] = SAVE_KEY_PREFIX + resourceData.key.ToString();
            }

            foreach (var resourceData in config.GetAllResources())
            {
                var saveKey = GetSaveKey(resourceData.key);
                saveSystem.RegisterKey(saveKey);
                resourceAmounts[resourceData.key] = LoadAmount(resourceData.key);
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
                        resourceAmounts[resourceData.key] = convertedAmt;
                        SaveAmount(resourceData.key, convertedAmt);
                    }
                }
                saveSystem.Save(FIRST_INIT_KEY, true);
            }
        }

        private string GetSaveKey(ResourceType id)
        {
            if (cachedSaveKeys.TryGetValue(id, out var key))
            {
                return key;
            }
            string newKey = SAVE_KEY_PREFIX + id.ToString();
            cachedSaveKeys[id] = newKey;
            return newKey;
        }

        private T LoadAmount(ResourceType id)
        {
            var saveKey = GetSaveKey(id);
         
            long savedLong = saveSystem.Load<long>(saveKey, 0L);

            return converter.FromLong(savedLong);
        }

        private void SaveAmount(ResourceType id, T amount)
        {
            var saveKey = GetSaveKey(id);

            long longAmount = converter.ToLong(amount);
            saveSystem.Save<long>(saveKey, longAmount);
        }

        public T GetAmount(ResourceType id)
        {
            return resourceAmounts.TryGetValue(id, out var amount) ? amount : converter.Zero;
        }

        public void AddResource(ResourceType id, T amount, bool delayUpdate = false)
        {
            if (converter.IsLessThan(amount, converter.Zero)) return;

            var currentAmount = GetAmount(id);
            var newAmount = converter.Add(currentAmount, amount);

            var maxStack = GetMaxStack(id);
            if (maxStack > 0)
            {
                long longNewAmount = converter.ToLong(newAmount);
                if (longNewAmount > maxStack)
                {
                    newAmount = converter.FromLong(maxStack);
                    eventService.Publish(ResourceEventType.ResourceMaxStackReached, this, id);
                }
            }

            resourceAmounts[id] = newAmount;
            SaveAmount(id, newAmount);

            var changeData = new ResourceChangeData<T>(id, currentAmount, newAmount, delayUpdate);
            eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
            eventService.Publish(ResourceEventType.ResourceAdded, this, changeData);
        }

        public bool SpendResource(ResourceType id, T amount)
        {
            if (converter.IsLessThan(amount, converter.Zero)) return false;

            if (_infiniteStatus != null && _infiniteStatus.IsCurrentlyInfinite(id))
            {
                var currentAmt = GetAmount(id);
                var changeData = new ResourceChangeData<T>(id, currentAmt, currentAmt); 
        
                eventService.Publish(ResourceEventType.ResourceSpent, this, changeData);
                return true; 
            }

            var currentAmount = GetAmount(id);
            if (converter.IsLessThan(currentAmount, amount))
            {
                eventService.Publish(ResourceEventType.ResourceInsufficient, this, id);
                return false;
            }

            var newAmount = converter.Subtract(currentAmount, amount);
            resourceAmounts[id] = newAmount;
            SaveAmount(id, newAmount);

            var changeDataNormal = new ResourceChangeData<T>(id, currentAmount, newAmount);
            eventService.Publish(ResourceEventType.ResourceChanged, this, changeDataNormal);
            eventService.Publish(ResourceEventType.ResourceSpent, this, changeDataNormal);

            return true;
        }

        public ResourceData GetResourceData(ResourceType id)
        {
            return config.GetResourceData(id);
        }

        public void Cleanup()
        {
            resourceAmounts.Clear();
            cachedSaveKeys.Clear();
        }

        public bool SetMaxStack(ResourceType id, long newMaxStack)
        {
            var resourceData = config.GetResourceData(id);
            if (resourceData == null)
            {
                Debug.LogWarning($"[ResourceModel] Cannot set max stack: Resource {id} not found");
                return false;
            }

            resourceData.MaxStack = newMaxStack;

            if (newMaxStack > 0)
            {
                var currentAmount = GetAmount(id);
                long longCurrentAmount = converter.ToLong(currentAmount);
                if (longCurrentAmount > newMaxStack)
                {
                    var newAmount = converter.FromLong(newMaxStack);
                    resourceAmounts[id] = newAmount;
                    SaveAmount(id, newAmount);

                    var changeData = new ResourceChangeData<T>(id, currentAmount, newAmount);
                    eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
                }
            }

            return true;
        }

        public long GetMaxStack(ResourceType id)
        {
            var resourceData = config.GetResourceData(id);
            return resourceData?.MaxStack ?? 0;
        }
    }
}