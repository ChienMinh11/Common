using System;
using System.Collections.Generic;
using MyFramework;
using UnityEngine;

namespace GameCore.Runtime
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
        private readonly INumberConverter<T> converter;
        private readonly IEventService eventService;
        private ResourceConfig config;
        private ISaveSystem saveSystem;

        private readonly Dictionary<ResourceType, T> resourceAmounts = new();

        public ResourceModel(INumberConverter<T> converter, IEventService eventService, ISaveSystem saveSystem)
        {
            this.converter = converter;
            this.eventService = eventService;
            this.saveSystem = saveSystem;
        }

        public void Initialize(ResourceConfig config)
        {
            this.config = config;
            config.Initialize();
           
            saveSystem.RegisterKey(FIRST_INIT_KEY);
            foreach (var resourceData in config.GetAllResources())
            {
                var saveKey = GetSaveKey(resourceData.key);
                saveSystem.RegisterKey(saveKey);
                resourceAmounts[resourceData.key] = LoadAmount(resourceData.key);
            }
        }

        public void InitializeDefaultValues()
        {
            var isFirstInit = saveSystem.Load<bool>(FIRST_INIT_KEY, true);
    
            if (isFirstInit)
            {
                foreach (var resourceData in config.GetAllResources())
                {
                    if (resourceData.DefaultAmount > 0)
                    {
                        var defaultValue = converter.Parse(resourceData.DefaultAmount.ToString());
                        resourceAmounts[resourceData.key] = defaultValue;
                        SaveAmount(resourceData.key, defaultValue);
                
                        var changeData = new ResourceChangeData<T>(resourceData.key, converter.Zero, defaultValue);
                        eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
                    }
                }
                saveSystem.Save(FIRST_INIT_KEY, false);
                Debug.Log("[ResourceModel] First-time initialization completed");
            }
        }

        private string GetSaveKey(ResourceType id)
        {
            return $"{SAVE_KEY_PREFIX}{id}";
        }

        private T LoadAmount(ResourceType id)
        {
            try
            {
                var saveKey = GetSaveKey(id);
                var savedValue = saveSystem.Load<object>(saveKey, "0");

                string stringValue = savedValue == null ? "0" : (savedValue is string str ? str : savedValue.ToString());
                return converter.Parse(stringValue);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Cannot load amount for {id}, using default value. Error: {e.Message}");
                return converter.Zero;
            }
        }

        private void SaveAmount(ResourceType id, T amount)
        {
            var saveKey = GetSaveKey(id);
            saveSystem.Save(saveKey, converter.ToString(amount));
        }

        public bool AddResource(ResourceType id, T amount, bool delayUpdate = false)
        {
            var resourceData = config.GetResourceData(id);
            if (resourceData == null) return false;

            var oldAmount = resourceAmounts[id];

            if (resourceData.MaxStack > 0)
            {
                var currentAmountStr = converter.ToString(oldAmount);
                if (long.TryParse(currentAmountStr, out var currentAmount))
                {
                    if (currentAmount >= resourceData.MaxStack)
                    {
                        eventService.PublishEvent(ResourceEventType.ResourceMaxStackReached, this);
                        return false;
                    }

                    var amountToAddStr = converter.ToString(amount);
                    if (long.TryParse(amountToAddStr, out var amountToAdd))
                    {
                        var potentialNewAmount = currentAmount + amountToAdd;
                        if (potentialNewAmount > resourceData.MaxStack)
                            amount = converter.Parse((resourceData.MaxStack - currentAmount).ToString());
                    }
                }
            }

            var newAmount = converter.Add(oldAmount, amount);
            if (converter.IsLessThan(newAmount, oldAmount)) newAmount = converter.MaxValue;

            resourceAmounts[id] = newAmount;
            SaveAmount(id, newAmount);
            
            var resourceChangeData = new ResourceChangeDataWithDelay<T>(id, oldAmount, newAmount, delayUpdate);
            eventService.Publish(ResourceEventType.ResourceChanged, this, resourceChangeData);
            eventService.Publish(ResourceEventType.ResourceAdded, this, resourceChangeData);

            return true;
        }

        public bool SpendResource(ResourceType id, T amount)
        {
            if (!resourceAmounts.ContainsKey(id))
            {
                eventService.PublishEvent(ResourceEventType.ResourceInsufficient, this);
                return false;
            }

            if (converter.IsLessThan(resourceAmounts[id], amount))
            {
                eventService.PublishEvent(ResourceEventType.ResourceInsufficient, this);
                return false;
            }

            var oldAmount = resourceAmounts[id];
            var newAmount = converter.Subtract(resourceAmounts[id], amount);
            resourceAmounts[id] = newAmount;
            SaveAmount(id, newAmount);
            
            var changeData = new ResourceChangeData<T>(id, oldAmount, newAmount);
            eventService.Publish(ResourceEventType.ResourceChanged, this, changeData);
            eventService.Publish(ResourceEventType.ResourceSpent, this, changeData);

            return true;
        }

        public T GetAmount(ResourceType id)
        {
            return resourceAmounts.TryGetValue(id, out var amount) ? amount : converter.Zero;
        }

        public ResourceData GetResourceData(ResourceType id)
        {
            return config.GetResourceData(id);
        }

        public void Cleanup()
        {
           
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
                var currentAmount = resourceAmounts[id];
                var currentAmountStr = converter.ToString(currentAmount);
                if (long.TryParse(currentAmountStr, out var longCurrentAmount))
                    if (longCurrentAmount > newMaxStack)
                    {
                        var newAmount = converter.Parse(newMaxStack.ToString());
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