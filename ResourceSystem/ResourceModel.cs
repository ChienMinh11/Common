using System;
using System.Collections.Generic;
using MyFramework;
using UnityEngine;

namespace GameCore.Runtime
{
    public class ResourceModel<T>
    {
        private const string SAVE_KEY_PREFIX = "Resource_";
        private const string INFINITE_SAVE_KEY_PREFIX = "InfiniteResource_";
        private const string FIRST_INIT_KEY = "ResourceFirstInit";
        private readonly INumberConverter<T> converter;
        private readonly IEventService eventService;
        private ResourceConfig config;

        private readonly Dictionary<ResourceType, InfiniteResourceData> infiniteResources = new();
        private readonly Dictionary<ResourceType, T> resourceAmounts = new();

        public ResourceModel(INumberConverter<T> converter, IEventService eventService)
        {
            this.converter = converter;
            this.eventService = eventService;
        }

        public void Initialize(ResourceConfig config)
        {
            this.config = config;
            config.Initialize();

            var saveSystem = SaveSystem.Instance;
            saveSystem.RegisterKey(FIRST_INIT_KEY);
            foreach (var resourceData in config.GetAllResources())
            {
                var saveKey = GetSaveKey(resourceData.key);
                saveSystem.RegisterKey(saveKey);
                resourceAmounts[resourceData.key] = LoadAmount(resourceData.key);
                var infiniteSaveKey = GetInfiniteSaveKey(resourceData.key);
                saveSystem.RegisterKey(infiniteSaveKey);
            }

            LoadInfiniteResources();
        }
        public void InitializeDefaultValues()
        {
            var saveSystem = SaveSystem.Instance;
    
            // Kiểm tra xem đã khởi tạo lần đầu chưa
            var isFirstInit = saveSystem.Load<bool>(FIRST_INIT_KEY, true);
    
            if (isFirstInit)
            {
                foreach (var resourceData in config.GetAllResources())
                {
                    if (resourceData.DefaultAmount > 0)
                    {
                        var defaultValue = converter.Parse(resourceData.DefaultAmount.ToString());
                
                        // Set giá trị ban đầu
                        resourceAmounts[resourceData.key] = defaultValue;
                        SaveAmount(resourceData.key, defaultValue);
                
                        // Publish event để UI cập nhật
                        var changeData = new ResourceChangeData<T>(resourceData.key, converter.Zero, defaultValue);
                        eventService.Publish(SystemEventType.ResourceChanged, this, changeData);
                    }
                }
        
                // Đánh dấu đã khởi tạo lần đầu
                saveSystem.Save(FIRST_INIT_KEY, false);
        
                Debug.Log("[ResourceModel] First-time initialization completed");
            }
        }

        private string GetSaveKey(ResourceType id)
        {
            return $"{SAVE_KEY_PREFIX}{id}";
        }

        private string GetInfiniteSaveKey(ResourceType id)
        {
            return $"{INFINITE_SAVE_KEY_PREFIX}{id}";
        }

        private T LoadAmount(ResourceType id)
        {
            try
            {
                var saveKey = GetSaveKey(id);
                var savedValue = SaveSystem.Instance.Load<object>(saveKey, "0");

                string stringValue;
                if (savedValue == null)
                    stringValue = "0";
                else if (savedValue is string str)
                    stringValue = str;
                else
                    stringValue = savedValue.ToString();

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
            SaveSystem.Instance.Save(saveKey, converter.ToString(amount));
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
                        eventService.PublishEvent(SystemEventType.ResourceMaxStackReached, this);
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

            ResourceChangeDataWithDelay<T> resourceChangeData;

            if (IsInfiniteResource(id))
            {
                resourceChangeData = new ResourceChangeDataWithDelay<T>(id, oldAmount, oldAmount, delayUpdate);
                eventService.Publish(SystemEventType.ResourceChanged, this, resourceChangeData);
                eventService.Publish(SystemEventType.ResourceAdded, this, resourceChangeData);
                return true;
            }

            var newAmount = converter.Add(oldAmount, amount);

            if (converter.IsLessThan(newAmount, oldAmount)) newAmount = converter.MaxValue;

            resourceAmounts[id] = newAmount;
            SaveAmount(id, newAmount);
            resourceChangeData = new ResourceChangeDataWithDelay<T>(id, oldAmount, newAmount, delayUpdate);
            eventService.Publish(SystemEventType.ResourceChanged, this, resourceChangeData);
            eventService.Publish(SystemEventType.ResourceAdded, this, resourceChangeData);

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
            eventService.ClearSource(this);
            infiniteResources.Clear();
        }

        private void SaveInfiniteResource(ResourceType id, InfiniteResourceData data)
        {
            var saveKey = GetInfiniteSaveKey(id);

            try
            {
                if (data == null || !data.IsActive)
                {
                    SaveSystem.Instance.Save<object>(saveKey, null);
                    return;
                }

                var saveData = new SavedInfiniteResourceData(
                    id,
                    data.Duration,
                    data.StartTimeTicks,
                    data.LastValidationTicks // Lưu thêm LastValidationTicks
                );

                var jsonData = JsonUtility.ToJson(saveData);
                SaveSystem.Instance.Save<object>(saveKey, jsonData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving infinite resource data for {id}: {e.Message}");
            }
        }

        private void LoadInfiniteResources()
        {
            foreach (var resourceData in config.GetAllResources())
            {
                var saveKey = GetInfiniteSaveKey(resourceData.key);

                try
                {
                    var savedValue = SaveSystem.Instance.Load<object>(saveKey);
                    if (savedValue == null) continue;

                    var jsonData = savedValue is string str ? str : savedValue.ToString();
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        try
                        {
                            var savedData = JsonUtility.FromJson<SavedInfiniteResourceData>(jsonData);
                            if (savedData != null)
                            {
                                var infiniteData = new InfiniteResourceData(
                                    savedData.ResourceType,
                                    savedData.Duration,
                                    savedData.StartTimeTicks
                                );

                                // Khôi phục LastValidationTicks
                                infiniteData.LastValidationTicks = savedData.LastValidationTicks;
                        
                                if (infiniteData.RemainingTime > 0)
                                {
                                    infiniteResources[resourceData.key] = infiniteData;
                                    eventService.Publish(SystemEventType.ResourceInfiniteStatusChanged, this,
                                        new ResourceInfiniteStatusData(resourceData.key, true,
                                            infiniteData.RemainingTime));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[ResourceModel] Failed to parse infinite data for {resourceData.key}: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ResourceModel] Failed to load infinite resource for {resourceData.key}: {e.Message}");
                }
            }
        }

        public void SetInfiniteResource(ResourceType id, float duration, bool delayUpdate = false)
        {
            if (infiniteResources.TryGetValue(id, out var existingData))
                existingData.ExtendDuration(duration);
            else
                infiniteResources[id] = new InfiniteResourceData(id, duration);

            SaveInfiniteResource(id, infiniteResources[id]);
            eventService.Publish(SystemEventType.ResourceInfiniteStatusChanged, this,
                new ResourceInfiniteStatusData(id, true, duration, delayUpdate));
        }

        public bool IsInfiniteResource(ResourceType id)
        {
            return infiniteResources.TryGetValue(id, out var data) && data.IsActive;
        }

        public void RemoveInfiniteResource(ResourceType id)
        {
            if (infiniteResources.Remove(id))
            {
                SaveInfiniteResource(id, null);
                eventService.Publish(SystemEventType.ResourceInfiniteStatusChanged, this,
                    new ResourceInfiniteStatusData(id, false, 0));
            }
        }

        public bool SpendResource(ResourceType id, T amount)
        {
            if (IsInfiniteResource(id))
            {
                var zeroChangeData = new ResourceChangeData<T>(id, resourceAmounts[id], resourceAmounts[id]);
                eventService.Publish(SystemEventType.ResourceSpent, this, zeroChangeData);
                return true;
            }

            if (!resourceAmounts.ContainsKey(id))
            {
                eventService.PublishEvent(SystemEventType.ResourceInsufficient, this);
                return false;
            }

            if (converter.IsLessThan(resourceAmounts[id], amount))
            {
                eventService.PublishEvent(SystemEventType.ResourceInsufficient, this);
                return false;
            }

            // Process the spend
            var oldAmount = resourceAmounts[id];
            var newAmount = converter.Subtract(resourceAmounts[id], amount);
            resourceAmounts[id] = newAmount;
            SaveAmount(id, newAmount);
            var changeData = new ResourceChangeData<T>(id, oldAmount, newAmount);
            eventService.Publish(SystemEventType.ResourceChanged, this, changeData);
            eventService.Publish(SystemEventType.ResourceSpent, this, changeData);

            return true;
        }

        public InfiniteResourceData GetInfiniteResourceData(ResourceType id)
        {
            if (infiniteResources.TryGetValue(id, out var data) && data.IsActive) return data;
            return null;
        }

        public bool SetMaxStack(ResourceType id, long newMaxStack)
        {
            var resourceData = config.GetResourceData(id);
            if (resourceData == null)
            {
                Debug.LogWarning($"[ResourceModel] Cannot set max stack: Resource {id} not found");
                return false;
            }

            // Set the new max stack
            resourceData.MaxStack = newMaxStack;

            // If current amount exceeds new max stack, adjust it down
            if (newMaxStack > 0)
            {
                var currentAmount = resourceAmounts[id];
                var currentAmountStr = converter.ToString(currentAmount);
                if (long.TryParse(currentAmountStr, out var longCurrentAmount))
                    if (longCurrentAmount > newMaxStack)
                    {
                        // Adjust current amount to new max stack
                        var newAmount = converter.Parse(newMaxStack.ToString());
                        resourceAmounts[id] = newAmount;
                        SaveAmount(id, newAmount);

                        // Notify about the resource change
                        var changeData = new ResourceChangeData<T>(id, currentAmount, newAmount);
                        eventService.Publish(SystemEventType.ResourceChanged, this, changeData);
                    }
            }

            return true;
        }
        public long GetMaxStack(ResourceType id)
        {
            var resourceData = config.GetResourceData(id);
            return resourceData?.MaxStack ?? 0;
        }
        
        public void ResetAllInfiniteResources()
        {
            var resourceTypesToRemove = new List<ResourceType>(infiniteResources.Keys);
    
            foreach (var resourceType in resourceTypesToRemove)
            {
                RemoveInfiniteResource(resourceType);
            }
    
            Debug.Log($"[ResourceModel] Reset all infinite resources. Removed {resourceTypesToRemove.Count} infinite resources.");
        }
    }
}