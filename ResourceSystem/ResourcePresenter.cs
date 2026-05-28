using System;
using R3;
using UnityEngine;

namespace ChieChie.Core
{
    public class ResourcePresenter<T> : IResourcePresenter
    {
        private readonly ResourceModel<T> model;
        private readonly IResourceView view;
        private readonly INumberConverter<T> converter;
        private readonly IEventService eventService;

        private IDisposable resourceChangeSubscription;
        private IDisposable resourceInsufficientSubscription;

        private readonly ResourceUpdateQueue updateQueue;
        private ResourceData currentResourceData;

        public ResourceType ResourceId { get; }
        public bool HasPendingUpdates => updateQueue.HasPendingUpdates;

        public ResourcePresenter(
            ResourceModel<T> model,
            IResourceView view,
            ResourceType resourceId,
            INumberConverter<T> converter,
            IEventService eventService)
        {
            this.model = model;
            this.view = view;
            this.ResourceId = resourceId;
            this.converter = converter;
            this.eventService = eventService;
            this.updateQueue = new ResourceUpdateQueue();

            SubscribeToEvents();
            UpdateView();
        }

        private void SubscribeToEvents()
        {
            resourceChangeSubscription = eventService.Observe<ResourceChangeData<T>, ResourceEventType>(
                ResourceEventType.ResourceChanged,
                model
            ).Subscribe(OnResourceChanged);

            resourceInsufficientSubscription = eventService.ObserveEvent(
                ResourceEventType.ResourceInsufficient,
                model
            ).Subscribe(_ => view.ShowInsufficientMessage());
        }

        private void OnResourceChanged(ResourceChangeData<T> changeData)
        {
            if (changeData.ResourceId != ResourceId) return;

            long oldDisplay = converter.ToLong(changeData.OldAmount);
            long newDisplay = converter.ToLong(changeData.NewAmount);

            if (changeData.DelayUpdate)
                updateQueue.EnqueueUpdate(oldDisplay, newDisplay);
            else
            {
                updateQueue.Clear();
                view.SetResourceAmount(newDisplay);
            }
        }

        public void ProcessPendingUpdates()
        {
            var update = updateQueue.ProcessNextUpdate(ResourceId);
    
            if (update != null)
            {
                var data = model.GetResourceData(update.ResourceId);
                if (data != null)
                {
                    view.SetResourceAmount(update.Amount);
                    view.SetResourceIcon(data.icon);
                    view.SetResourceName(data.displayName);

                    if (data.MaxStack > 0 && update.Amount >= data.MaxStack)
                    {
                        view.OnMaxStackReached(update.ResourceId);
                    }
                }
            }
        }

        public void ForceUpdateView()
        {
            currentResourceData = model.GetResourceData(ResourceId);
            if (currentResourceData == null) return;

            updateQueue.Clear();

            // TỐI ƯU: Lấy giá trị trực tiếp đẩy sang View mà không đi vòng qua string
            long displayAmount = converter.ToLong(model.GetAmount(ResourceId));
            view.SetResourceAmountWithoutAnimation(displayAmount);

            view.SetResourceIcon(currentResourceData.icon);
            view.SetResourceName(currentResourceData.displayName);
        }

        private void UpdateView()
        {
            currentResourceData = model.GetResourceData(ResourceId);
            if (currentResourceData == null) return;

            view.SetResourceIcon(currentResourceData.icon);

            // TỐI ƯU: Đọc thẳng giá trị long, loại bỏ hoàn toàn string allocation
            long displayAmount = converter.ToLong(model.GetAmount(ResourceId));
            view.SetResourceAmount(displayAmount);
            view.SetResourceName(currentResourceData.displayName);
        }

        public bool TrySpendResource(T amount) => model.SpendResource(ResourceId, amount);
        public void AddResource(T amount, bool delayUpdate = false) => model.AddResource(ResourceId, amount, delayUpdate);

        public void Cleanup()
        {
            resourceChangeSubscription?.Dispose();
            resourceChangeSubscription = null;

            resourceInsufficientSubscription?.Dispose();
            resourceInsufficientSubscription = null;

            updateQueue.Clear();
        }
    }
}