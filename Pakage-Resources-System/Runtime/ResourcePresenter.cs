using System;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourcePresenter<T> : IResourcePresenter
    {
        private readonly ResourceModel<T> _model;
        private readonly IResourceView _view;
        private readonly INumberConverter<T> _converter;
        private readonly IEventService _eventService;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;
        private readonly IIconProvider _iconProvider;

        private IDisposable _resourceChangeSubscription;
        private IDisposable _resourceInsufficientSubscription;

        private readonly ResourceUpdateQueue _updateQueue;
        private ResourceData _currentResourceData;

        private readonly CancellationTokenSource _countdownCts;

        public ResourceType ResourceId { get; }
        public bool HasPendingUpdates => _updateQueue.HasPendingUpdates;

        public ResourcePresenter(
            ResourceModel<T> model,
            IResourceView view,
            ResourceType resourceId,
            INumberConverter<T> converter,
            IEventService eventService,
            IReadOnlyInfiniteStatus infiniteStatus,
            IIconProvider iconProvider)
        {
            _model = model;
            _view = view;
            ResourceId = resourceId;
            _converter = converter;
            _eventService = eventService;
            _infiniteStatus = infiniteStatus;
            _iconProvider = iconProvider;
            this._updateQueue = new ResourceUpdateQueue();

            SubscribeToEvents();
            UpdateView();

            _countdownCts = new CancellationTokenSource();

            CancellationToken linkedToken = _countdownCts.Token;
            if (view is MonoBehaviour monoView)
            {
                linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                    _countdownCts.Token,
                    monoView.GetCancellationTokenOnDestroy()
                ).Token;
            }

            StartInfiniteTimerAsync(linkedToken).Forget();
        }

        private async UniTaskVoid StartInfiniteTimerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_view == null || (_view is MonoBehaviour monoView && monoView == null))
                {
                    break;
                }

                bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceId);
                
                Sprite iconToSet = (isCurrentlyInfinite ) 
                    ? GetIconResourceReward(ResourceId,true)
                    : GetIconResourceReward(ResourceId,false);
                
                _view.SetResourceIcon(iconToSet);
                _view.SetInfiniteStatus(isCurrentlyInfinite);
                if (isCurrentlyInfinite)
                {
                    TimeSpan remaining = _infiniteStatus.GetRemainingInfiniteTime(ResourceId);
                    _view.UpdateInfinityRemainingTime(TimeFormatter.FormatRemainingTime(remaining));
                }

                if (!isCurrentlyInfinite)
                {
                    long displayAmount = _converter.ToLong(_model.GetAmount(ResourceId));
                    _view.SetResourceAmount(displayAmount);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1), delayTiming: PlayerLoopTiming.Update,
                    cancellationToken: token);
            }
        }

        private void SubscribeToEvents()
        {
            _resourceChangeSubscription = _eventService.Observe<ResourceChangeData<T>, ResourceEventType>(
                ResourceEventType.ResourceChanged, _model
            ).Subscribe(OnResourceChanged);

            _resourceInsufficientSubscription = _eventService.ObserveEvent(
                ResourceEventType.ResourceInsufficient, _model
            ).Subscribe(_ => _view.ShowInsufficientMessage());
        }

        private void OnResourceChanged(ResourceChangeData<T> changeData)
        {
            if (changeData.ResourceId != ResourceId) return;

            long oldDisplay = _converter.ToLong(changeData.OldAmount);
            long newDisplay = _converter.ToLong(changeData.NewAmount);

            if (changeData.DelayUpdate)
                _updateQueue.EnqueueUpdate(oldDisplay, newDisplay);
            else
            {
                _updateQueue.Clear();

                if (!_infiniteStatus.IsCurrentlyInfinite(ResourceId))
                {
                    _view.SetResourceAmount(newDisplay);
                }
            }
        }

        public void ProcessPendingUpdates()
        {
            var update = _updateQueue.ProcessNextUpdate(ResourceId);
            if (update != null)
            {
                var data = _model.GetResourceData(update.ResourceId);
                if (data != null)
                {
                    _view.SetResourceAmount(update.Amount);
                    _view.SetResourceIcon(GetIconResourceReward(ResourceId,false));
                    _view.SetResourceName(data.displayName);

                    if (data.MaxStack > 0 && update.Amount >= data.MaxStack)
                    {
                        _view.OnMaxStackReached(update.ResourceId);
                    }
                }
            }
        }

        public void ForceUpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceId);
            if (_currentResourceData == null) return;

            _updateQueue.Clear();
            long displayAmount = _converter.ToLong(_model.GetAmount(ResourceId));
            _view.SetResourceAmountWithoutAnimation(displayAmount);

            if(_infiniteStatus.IsCurrentlyInfinite(ResourceId))
                _view.SetResourceIcon(GetIconResourceReward(ResourceId,false));
            _view.SetResourceName(_currentResourceData.displayName);
        }

        private void UpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceId);
            if (_currentResourceData == null) return;
            bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceId);
            Sprite iconToSet = (isCurrentlyInfinite) 
                ? GetIconResourceReward(ResourceId,true) 
                : GetIconResourceReward(ResourceId,false);

            _view.SetResourceIcon(iconToSet);
            long displayAmount = _converter.ToLong(_model.GetAmount(ResourceId));
            _view.SetResourceAmount(displayAmount);
            _view.SetResourceName(_currentResourceData.displayName);
        }

        private Sprite GetIconResourceReward(ResourceType resourceType, bool isInfinite)
        {
            if (_iconProvider == null) return null;
         
            return _iconProvider.GetRewardIcon(resourceType, isInfinite);
        }

        public void Cleanup()
        {
            _resourceChangeSubscription?.Dispose();
            _resourceInsufficientSubscription?.Dispose();
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _updateQueue.Clear();
        }
    }
}