using System;
using GameCore.Runtime._Core.GameCore.Runtime.Scripts.ResourceSystem;
using MyFramework;

namespace GameCore.Runtime
{
    public class ResourcePresenter<T>
    {
        private readonly ResourceModel<T> model;
        private readonly IResourceView view;
        private readonly ResourceType resourceId;
        private readonly INumberConverter<T> converter;
        private readonly IEventService eventService;
        private IDisposable resourceChangeSubscription;
        private IDisposable infiniteStatusSubscription;
        private readonly ResourceUpdateQueue updateQueue;
        private readonly ResourceUpdateQueue infiniteUpdateQueue;
        private ResourceData currentResourceData;
        private bool isInfiniteActive;
        private float infiniteStartTime;
        private float infiniteDuration;
        private int timerHash;
        private TimeManager timeManager;
        public bool HasPendingUpdates => updateQueue.HasPendingUpdates;
        
        public ResourcePresenter(
            ResourceModel<T> model, 
            IResourceView view, 
            ResourceType resourceId, 
            INumberConverter<T> converter,
            IEventService eventService,
            TimeManager timeManager)
        {
            this.model = model;
            this.view = view;
            this.resourceId = resourceId;
            this.converter = converter;
            this.eventService = eventService;
            this.timeManager = timeManager;
            this.updateQueue = new ResourceUpdateQueue();
            this.infiniteUpdateQueue = new ResourceUpdateQueue();
            this.timerHash = $"ResourceTimer_{resourceId}_{GetHashCode()}_{System.DateTime.Now.Ticks}".GetHashCode();
            SubscribeToEvents();
            UpdateView();
            if (model.IsInfiniteResource(resourceId))
            {
                StartInfiniteTimeUpdate();
            }
        }

        private void SubscribeToEvents()
        {
            resourceChangeSubscription = eventService.Subscribe<ResourceChangeData<T>, SystemEventType>(
                SystemEventType.ResourceChanged,
                model,
                OnResourceChanged
            );

            infiniteStatusSubscription = eventService.Subscribe<ResourceInfiniteStatusData, SystemEventType>(
                SystemEventType.ResourceInfiniteStatusChanged,
                model,
                OnInfiniteStatusChanged
            );

            eventService.SubscribeEvent(
                SystemEventType.ResourceInsufficient,
                model,
                () => view.ShowInsufficientMessage()
            );
        }
        private void StartInfiniteTimeUpdate()
        {
            isInfiniteActive = true;
    
            // Get remaining time from model
            if (model is ResourceModel<T> resourceModel)
            {
                var infiniteData = resourceModel.GetInfiniteResourceData(resourceId);
                if (infiniteData != null)
                {
                    float remainingTime = infiniteData.RemainingTime;
                    if (remainingTime > 0)
                    {
                        timeManager.CreateCountdownTimer(
                            timerHash,
                            remainingTime,
                            (progress) =>
                            {
                                float currentRemaining = remainingTime * (1 - progress);
                                view.UpdateInfiniteTimeRemaining(currentRemaining);
                            },
                            () =>
                            {
                                isInfiniteActive = false;
                                view.UpdateInfiniteTimeRemaining(0);
                                view.SetInfiniteState(false);
                            }
                        );
                    }
                    else
                    {
                        // Handle case when remaining time is 0
                        isInfiniteActive = false;
                        view.UpdateInfiniteTimeRemaining(0);
                        view.SetInfiniteState(false);
                    }
                }
            }
        }

        private void OnInfiniteStatusChanged(ResourceInfiniteStatusData statusData)
        {
            if (statusData.ResourceId == resourceId)
            {
                if (statusData.Duration <= 0)
                {
                    view.SetInfiniteState(false);
                    timeManager.StopTimer(timerHash);
                    isInfiniteActive = false;
                    view.UpdateInfiniteTimeRemaining(0);
                    return;
                }

                if (statusData.DelayUpdate)
                {
                    infiniteUpdateQueue.EnqueueUpdate(new ResourceUpdateData(resourceId, -1) 
                    { 
                        IsInfinite = true,
                        InfiniteDuration = statusData.Duration
                    });
                }
                else
                {
                    view.SetInfiniteState(statusData.IsInfinite);
                    if (statusData.IsInfinite)
                    {
                        timeManager.StopTimer(timerHash);
                        infiniteDuration = statusData.Duration;
                        StartInfiniteTimeUpdate();
                    }
                    else
                    {
                        timeManager.StopTimer(timerHash);
                        isInfiniteActive = false;
                        view.UpdateInfiniteTimeRemaining(0);
                    }
                }
            }
        }

        private void OnResourceChanged(ResourceChangeData<T> changeData)
        {
            if (changeData.ResourceId == resourceId)
            {
                var updateData = changeData as ResourceChangeDataWithDelay<T>;
                if (updateData?.DelayUpdate ?? false)
                {
                    // DelayUpdate = true -> thêm vào queue để chạy animation sau
                    string amountStr = converter.ToString(changeData.NewAmount);
                    if (long.TryParse(amountStr, out long amount))
                    {
                        updateQueue.EnqueueUpdate(new ResourceUpdateData(resourceId, amount));
                    }
                }
                else
                {
                    // DelayUpdate = false -> cập nhật trực tiếp không animation
                    string amountStr = converter.ToString(changeData.NewAmount);
                    if (long.TryParse(amountStr, out long displayAmount))
                    {
                        view.SetResourceAmountWithoutAnimation(displayAmount); // Dùng method không animation
        
                        // Kiểm tra maxStack sau khi cập nhật UI
                        var resourceData = model.GetResourceData(resourceId);
                        if (resourceData != null && resourceData.MaxStack > 0)
                        {
                            if (displayAmount >= resourceData.MaxStack)
                            {
                                view.OnMaxStackReached(changeData.ResourceId);
                            }
                        }
                    }
                }
            }
        }

        public void ProcessPendingUpdates()
        {
            var update = updateQueue.ProcessNextUpdate();
            if (update != null)
            {
                var data = model.GetResourceData(update.ResourceId);
                if (data != null)
                {
                    // Gọi SetResourceAmount sẽ trigger animation
                    view.SetResourceAmount(update.Amount);
                    view.SetResourceIcon(data.icon);
                    view.SetResourceName(data.displayName);

                    // Kiểm tra maxStack sau khi xử lý update từ queue
                    if (data.MaxStack > 0 && update.Amount >= data.MaxStack)
                    {
                        view.OnMaxStackReached(update.ResourceId);
                    }
                }
            }

            var infiniteUpdate = infiniteUpdateQueue.ProcessNextUpdate();
            if (infiniteUpdate != null && infiniteUpdate.IsInfinite)
            {
                view.SetInfiniteState(true);
                timeManager.StopTimer(timerHash);
                infiniteDuration = infiniteUpdate.InfiniteDuration;
                StartInfiniteTimeUpdate();
            }
        }
        public void ForceUpdateView()
        {
            currentResourceData = model.GetResourceData(resourceId);
            if (currentResourceData == null) return;

            // XỬ LÝ TẤT CẢ pending updates trước
            while (infiniteUpdateQueue.HasPendingUpdates)
            {
                var infiniteUpdate = infiniteUpdateQueue.ProcessNextUpdate();
                if (infiniteUpdate != null && infiniteUpdate.IsInfinite)
                {
                    timeManager.StopTimer(timerHash);
                    infiniteDuration = infiniteUpdate.InfiniteDuration;
                    // Không start timer ở đây, sẽ start ở cuối
                }
            }

            // Clear animation queue
            updateQueue.Clear();

            // Update view directly
            string amountStr = converter.ToString(model.GetAmount(resourceId));
            if (long.TryParse(amountStr, out long displayAmount))
            {
                view.SetResourceAmountWithoutAnimation(displayAmount);
            }

            view.SetResourceIcon(currentResourceData.icon);
            view.SetResourceName(currentResourceData.displayName);
    
            if (currentResourceData.infiniteIcon != null)
            {
                view.SetInfiniteIcon(currentResourceData.infiniteIcon);
            }

            // Cập nhật infinite state SAU CÙNG
            bool isInfinite = model.IsInfiniteResource(resourceId);
            view.SetInfiniteState(isInfinite);
    
            if (isInfinite)
            {
                var infiniteData = model.GetInfiniteResourceData(resourceId);
                if (infiniteData != null)
                {
                    StartInfiniteTimeUpdate();
                }
            }
        }

        private void UpdateView()
        {
            currentResourceData = model.GetResourceData(resourceId);
            if (currentResourceData == null) return;

            view.SetResourceIcon(currentResourceData.icon);
            if (currentResourceData.infiniteIcon != null)
            {
                view.SetInfiniteIcon(currentResourceData.infiniteIcon);
            }

            // Set initial infinite state
            view.SetInfiniteState(model.IsInfiniteResource(resourceId));

            string amountStr = converter.ToString(model.GetAmount(resourceId));
            if (long.TryParse(amountStr, out long displayAmount))
            {
                view.SetResourceAmount(displayAmount);
                view.SetResourceName(currentResourceData.displayName);
            }
        }

        public bool TrySpendResource(T amount)
        {
            return model.SpendResource(resourceId, amount);
        }

        public void AddResource(T amount, bool delayUpdate = false)
        {
            model.AddResource(resourceId, amount, delayUpdate);
        }

        // Helper methods for common numeric types
        public bool TrySpendResource(int amount)
        {
            return TrySpendResource(converter.Parse(amount.ToString()));
        }

        public bool TrySpendResource(long amount)
        {
            return TrySpendResource(converter.Parse(amount.ToString()));
        }

        public void AddResource(int amount)
        {
            AddResource(converter.Parse(amount.ToString()));
        }

        public void AddResource(long amount)
        {
            AddResource(converter.Parse(amount.ToString()));
        }
        public bool SetMaxStack(long newMaxStack)
        {
            if (model is ResourceModel<T> resourceModel)
            {
                return resourceModel.SetMaxStack(resourceId, newMaxStack);
            }
            return false;
        }

        public void Cleanup()
        {
            resourceChangeSubscription?.Dispose();
            resourceChangeSubscription = null;
            infiniteStatusSubscription?.Dispose();
            infiniteStatusSubscription = null;
            updateQueue.Clear();
        }
    }
}