using System;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourcePresenter<T> : IResourcePresenter
    {
        private readonly ResourceModel<T> _model;
        private readonly IResourceView _view;
        private readonly INumberConverter<T> _converter;
        private readonly ResourceManager _infiniteStatus;

        private readonly ResourceUpdateQueue _updateQueue;
        private ResourceData _currentResourceData;
        public IResourceView View => _view;

        private readonly CancellationTokenSource _countdownCts;

        public string ResourceKey { get; }
        public bool HasPendingUpdates => _updateQueue.HasPendingUpdates;
        private bool _isInfiniteUpdateDelayed = false;
        private TimeSpan _delayedInfiniteDuration = TimeSpan.Zero;

        public ResourcePresenter(
            ResourceModel<T> model,
            IResourceView view,
            string resourceKey,
            INumberConverter<T> converter,
            ResourceManager infiniteStatus)
        {
            _model = model;
            _view = view;
            ResourceKey = resourceKey;
            _converter = converter;
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
            if (_view == null) return;
            _currentResourceData = _model.GetResourceData(ResourceKey);
            while (!token.IsCancellationRequested)
            {
                if (_view == null || (_view is MonoBehaviour monoView && monoView == null)) break;
              
                bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceKey) && !_isInfiniteUpdateDelayed;
                var iconToSet = SetIcon(isCurrentlyInfinite);

                _view.SetResourceIcon(iconToSet);
                _view.SetInfiniteStatus(isCurrentlyInfinite);
        
                if (isCurrentlyInfinite)
                {
                    TimeSpan remaining = _infiniteStatus.GetRemainingInfiniteTime(ResourceKey);
                    _view.UpdateInfinityRemainingTime(TimeFormatter.FormatRemainingTime(remaining));
                }
                else
                {
                    string exactKey = _currentResourceData != null ? _currentResourceData.ResourceId : ResourceKey;
                    long displayAmount = _converter.ToLong(_model.GetAmount(exactKey));
                    _view.SetResourceAmount(displayAmount);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1), delayTiming: PlayerLoopTiming.Update, cancellationToken: token);
            }
        }

        private Sprite SetIcon(bool isCurrentlyInfinite)
        {
            _currentResourceData = _model.GetResourceData(ResourceKey);
            Sprite iconToSet = _currentResourceData?.Icon;
            if (isCurrentlyInfinite && _currentResourceData != null && _currentResourceData.InfinityIcon != null)
            {
                iconToSet = _currentResourceData.InfinityIcon;
            }

            return iconToSet;
        }

        private void SubscribeToEvents()
        {
            _model.OnResourceChanged += OnResourceChanged;
            _model.OnResourceInsufficient += OnResourceInsufficient;
            _infiniteStatus.OnInfiniteAdded += OnInfiniteAddedFromManager;
        }

        private void OnResourceInsufficient(string resourceKey)
        {
            if (resourceKey == ResourceKey)
            {
                _view.ShowInsufficientMessage();
            }
        }

        private void OnResourceChanged(ResourceChangeData<T> changeData)
        {
            if (changeData.ResourceId != ResourceKey) return;

            long oldDisplay = _converter.ToLong(changeData.OldAmount);
            long newDisplay = _converter.ToLong(changeData.NewAmount);

            if (changeData.DelayUpdate)
                _updateQueue.EnqueueUpdate(oldDisplay, newDisplay);
            else
            {
                _updateQueue.Clear();

                if (!_infiniteStatus.IsCurrentlyInfinite(ResourceKey))
                {
                    _view.SetResourceAmountWithoutAnimation(newDisplay);
                }
            }
        }
        private void OnInfiniteAddedFromManager(string resourceKey, bool delayUpdate)
        {
            if (resourceKey != ResourceKey) return;

            if (delayUpdate)
            {
                _isInfiniteUpdateDelayed = true;
                _updateQueue.EnqueueUpdate(0, _converter.ToLong(_model.GetAmount(ResourceKey)));
            }
            else
            {
                _isInfiniteUpdateDelayed = false;
            }
        }

        public void ProcessPendingUpdates()
        {
           
            _isInfiniteUpdateDelayed = false;

            var update = _updateQueue.ProcessNextUpdate(ResourceKey);
            if (update != null)
            {
                var data = _model.GetResourceData(ResourceKey);
                if (data != null)
                {
                    bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceKey);
                  
                    if (!isCurrentlyInfinite)
                    {
                        _view.SetResourceAmount(update.Amount);
                    }
            
                    Sprite iconToSet = SetIcon(isCurrentlyInfinite);
                    _view.SetResourceIcon(iconToSet);
                    _view.SetResourceName(data.DisplayName);
                    _view.SetInfiniteStatus(isCurrentlyInfinite);

                    if (data.MaxStack > 0 && update.Amount >= data.MaxStack)
                    {
                        _view.OnMaxStackReached(ResourceKey);
                    }
                }
            }
        }
        
        public void ForceUpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceKey);
            if (_currentResourceData == null) return;

            _updateQueue.Clear();
            long displayAmount = _converter.ToLong(_model.GetAmount(_currentResourceData.ResourceId));
            _view.SetResourceAmountWithoutAnimation(displayAmount);
            bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceKey);
            Sprite iconToSet = SetIcon(isCurrentlyInfinite);
            _view.SetResourceIcon(iconToSet);
            _view.SetResourceName(_currentResourceData.DisplayName);
        }

        private void UpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceKey);
            if (_currentResourceData == null) return;
            bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceKey);
            Sprite iconToSet = SetIcon(isCurrentlyInfinite);
            _view.SetResourceIcon(iconToSet);
            long displayAmount = _converter.ToLong(_model.GetAmount(_currentResourceData.ResourceId));
            _view.SetResourceAmount(displayAmount);
            _view.SetResourceName(_currentResourceData.DisplayName);
        }

        public void Cleanup()
        {
            if (_model != null)
            {
                _model.OnResourceChanged -= OnResourceChanged;
                _model.OnResourceInsufficient -= OnResourceInsufficient;
            }
            _infiniteStatus.OnInfiniteAdded -= OnInfiniteAddedFromManager;
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _updateQueue.Clear();
        }
    }
}