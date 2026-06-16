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

        private IDisposable _resourceChangeSubscription;
        private IDisposable _resourceInsufficientSubscription;

        private readonly ResourceUpdateQueue _updateQueue;
        private ResourceData _currentResourceData;

        private readonly CancellationTokenSource _countdownCts;

        public string ResourceKey { get; }
        public int ResourceHash { get; }
        public bool HasPendingUpdates => _updateQueue.HasPendingUpdates;

        public ResourcePresenter(
            ResourceModel<T> model,
            IResourceView view,
            string resourceKey, 
            INumberConverter<T> converter,
            IEventService eventService,
            IReadOnlyInfiniteStatus infiniteStatus)
        {
            _model = model;
            _view = view;
            ResourceKey = resourceKey;
            
            ResourceHash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _converter = converter;
            _eventService = eventService;
            _infiniteStatus = infiniteStatus;
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

                bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceHash);
          
                _currentResourceData = _model.GetResourceData(ResourceHash);
                Sprite iconToSet = _currentResourceData?.Icon;
                if (isCurrentlyInfinite && _currentResourceData != null && _currentResourceData.InfinityIcon != null)
                {
                    iconToSet = _currentResourceData.InfinityIcon;
                }
                
                _view.SetResourceIcon(iconToSet);
                _view.SetInfiniteStatus(isCurrentlyInfinite);
                if (isCurrentlyInfinite)
                {
                    TimeSpan remaining = _infiniteStatus.GetRemainingInfiniteTime(ResourceHash);
                    _view.UpdateInfinityRemainingTime(TimeFormatter.FormatRemainingTime(remaining));
                }

                if (!isCurrentlyInfinite)
                {
                    long displayAmount = _converter.ToLong(_model.GetAmount(ResourceKey));
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
            if (changeData.ResourceId != ResourceHash) return;

            long oldDisplay = _converter.ToLong(changeData.OldAmount);
            long newDisplay = _converter.ToLong(changeData.NewAmount);

            if (changeData.DelayUpdate)
                _updateQueue.EnqueueUpdate(oldDisplay, newDisplay);
            else
            {
                _updateQueue.Clear();

                if (!_infiniteStatus.IsCurrentlyInfinite(ResourceHash))
                {
                    _view.SetResourceAmount(newDisplay);
                }
            }
        }

        public void ProcessPendingUpdates()
        {
            var update = _updateQueue.ProcessNextUpdate(ResourceHash); 
            if (update != null)
            {
                var data = _model.GetResourceData(ResourceHash);
                if (data != null)
                {
                    _view.SetResourceAmount(update.Amount);
                    _view.SetResourceIcon(data.Icon);
                    _view.SetResourceName(data.DisplayName);

                    if (data.MaxStack > 0 && update.Amount >= data.MaxStack)
                    {
                        _view.OnMaxStackReached(ResourceHash);
                    }
                }
            }
        }

        public void ForceUpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceHash);
            if (_currentResourceData == null) return;

            _updateQueue.Clear();
            long displayAmount = _converter.ToLong(_model.GetAmount(ResourceKey));
            _view.SetResourceAmountWithoutAnimation(displayAmount);

            _view.SetResourceIcon(_currentResourceData.Icon);
            _view.SetResourceName(_currentResourceData.DisplayName);
        }

        private void UpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceHash);
            if (_currentResourceData == null) return;

            _view.SetResourceIcon(_currentResourceData.Icon);
            long displayAmount = _converter.ToLong(_model.GetAmount(ResourceKey));
            _view.SetResourceAmount(displayAmount);
            _view.SetResourceName(_currentResourceData.DisplayName);
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