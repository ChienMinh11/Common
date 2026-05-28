using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        private readonly IReadOnlyInfiniteStatus infiniteStatus;

        private IDisposable resourceChangeSubscription;
        private IDisposable resourceInsufficientSubscription;

        private readonly ResourceUpdateQueue updateQueue;
        private ResourceData currentResourceData;

        private CancellationTokenSource countdownCts;

        public ResourceType ResourceId { get; }
        public bool HasPendingUpdates => updateQueue.HasPendingUpdates;

        public ResourcePresenter(
            ResourceModel<T> model,
            IResourceView view,
            ResourceType resourceId,
            INumberConverter<T> converter,
            IEventService eventService,
            IReadOnlyInfiniteStatus infiniteStatus)
        {
            this.model = model;
            this.view = view;
            this.ResourceId = resourceId;
            this.converter = converter;
            this.eventService = eventService;
            this.infiniteStatus = infiniteStatus;
            this.updateQueue = new ResourceUpdateQueue();

            SubscribeToEvents();
            UpdateView();

            countdownCts = new CancellationTokenSource();

            CancellationToken linkedToken = countdownCts.Token;
            if (view is MonoBehaviour monoView)
            {
                linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                    countdownCts.Token,
                    monoView.GetCancellationTokenOnDestroy()
                ).Token;
            }

            StartInfiniteTimerAsync(linkedToken).Forget();
        }

        private async UniTaskVoid StartInfiniteTimerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (view == null || (view is MonoBehaviour monoView && monoView == null))
                {
                    break;
                }

                bool isCurrentlyInfinite = infiniteStatus.IsCurrentlyInfinite(ResourceId);
                
                Sprite iconToSet = (isCurrentlyInfinite && currentResourceData.infinityIcon != null) 
                    ? currentResourceData.infinityIcon 
                    : currentResourceData.icon;
                
                view.SetResourceIcon(iconToSet);

                if (view is IInfiniteResourceView infiniteView)
                {
                    infiniteView.SetInfiniteStatus(isCurrentlyInfinite);
                    if (isCurrentlyInfinite)
                    {
                        TimeSpan remaining = infiniteStatus.GetRemainingInfiniteTime(ResourceId);
                        infiniteView.UpdateRemainingTimeText(CoreExtensions.FormatRemainingTime(remaining));
                    }
                }

                if (!isCurrentlyInfinite)
                {
                    long displayAmount = converter.ToLong(model.GetAmount(ResourceId));
                    view.SetResourceAmount(displayAmount);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1), delayTiming: PlayerLoopTiming.Update,
                    cancellationToken: token);
            }
        }

        private void SubscribeToEvents()
        {
            resourceChangeSubscription = eventService.Observe<ResourceChangeData<T>, ResourceEventType>(
                ResourceEventType.ResourceChanged, model
            ).Subscribe(OnResourceChanged);

            resourceInsufficientSubscription = eventService.ObserveEvent(
                ResourceEventType.ResourceInsufficient, model
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

                if (!infiniteStatus.IsCurrentlyInfinite(ResourceId))
                {
                    view.SetResourceAmount(newDisplay);
                }
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
            long displayAmount = converter.ToLong(model.GetAmount(ResourceId));
            view.SetResourceAmountWithoutAnimation(displayAmount);

            if(infiniteStatus.IsCurrentlyInfinite(ResourceId))
            view.SetResourceIcon(currentResourceData.icon);
            view.SetResourceName(currentResourceData.displayName);
        }

        private void UpdateView()
        {
            currentResourceData = model.GetResourceData(ResourceId);
            if (currentResourceData == null) return;
            bool isCurrentlyInfinite = infiniteStatus.IsCurrentlyInfinite(ResourceId);
            Sprite iconToSet = (isCurrentlyInfinite && currentResourceData.infinityIcon != null) 
                ? currentResourceData.infinityIcon 
                : currentResourceData.icon;

            view.SetResourceIcon(iconToSet);
            long displayAmount = converter.ToLong(model.GetAmount(ResourceId));
            view.SetResourceAmount(displayAmount);
            view.SetResourceName(currentResourceData.displayName);
        }

        public void Cleanup()
        {
            resourceChangeSubscription?.Dispose();
            resourceInsufficientSubscription?.Dispose();
            countdownCts?.Cancel();
            countdownCts?.Dispose();
            updateQueue.Clear();
        }
    }
}