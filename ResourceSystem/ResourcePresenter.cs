using System;
using MyFramework;
using R3;
using UnityEngine;

namespace GameCore.Runtime
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

        public ResourceType ResourceId { get; } // Thực hiện interface
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

            var updateData = changeData as ResourceChangeDataWithDelay<T>;
            if (updateData?.DelayUpdate ?? false)
            {
                string amountStr = converter.ToString(changeData.NewAmount);
                if (long.TryParse(amountStr, out long amount))
                {
                    updateQueue.EnqueueUpdate(new ResourceUpdateData(ResourceId, amount));
                }
            }
            else
            {
                string amountStr = converter.ToString(changeData.NewAmount);
                if (long.TryParse(amountStr, out long displayAmount))
                {
                    view.SetResourceAmountWithoutAnimation(displayAmount);

                    var resourceData = model.GetResourceData(ResourceId);
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

        public void ProcessPendingUpdates()
        {
            var update = updateQueue.ProcessNextUpdate();
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

            string amountStr = converter.ToString(model.GetAmount(ResourceId));
            if (long.TryParse(amountStr, out long displayAmount))
            {
                view.SetResourceAmountWithoutAnimation(displayAmount);
            }

            view.SetResourceIcon(currentResourceData.icon);
            view.SetResourceName(currentResourceData.displayName);
        }

        private void UpdateView()
        {
            currentResourceData = model.GetResourceData(ResourceId);
            if (currentResourceData == null) return;

            view.SetResourceIcon(currentResourceData.icon);

            string amountStr = converter.ToString(model.GetAmount(ResourceId));
            if (long.TryParse(amountStr, out long displayAmount))
            {
                view.SetResourceAmount(displayAmount);
                view.SetResourceName(currentResourceData.displayName);
            }
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