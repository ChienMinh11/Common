using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourcePresenter<T> : IResourcePresenter
    {
        private readonly ResourceModel<T> _model;
        private readonly IResourceView _view;
        private readonly INumberConverter<T> _converter;
        private readonly IReadOnlyInfiniteStatus _infiniteStatus;

        private readonly ResourceUpdateQueue _updateQueue;
        private ResourceData _currentResourceData;
        public IResourceView View => _view;

        private readonly CancellationTokenSource _countdownCts;

        public string ResourceKey { get; }
        public int ResourceHash { get; }
        public bool HasPendingUpdates => _updateQueue.HasPendingUpdates;

        // Bỏ IEventService ra khỏi constructor, thay thế bằng việc truyền trực tiếp ResourceManager hoặc lấy model từ manager
        public ResourcePresenter(
            ResourceModel<T> model,
            IResourceView view,
            string resourceKey,
            INumberConverter<T> converter,
            IReadOnlyInfiniteStatus infiniteStatus)
        {
            _model = model;
            _view = view;
            ResourceKey = resourceKey;
            ResourceHash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
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
            _currentResourceData = _model.GetResourceData(ResourceHash);
            while (!token.IsCancellationRequested)
            {
                if (_view == null || (_view is MonoBehaviour monoView && monoView == null))
                {
                    break;
                }

                bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceHash);
                var iconToSet = SetIcon(isCurrentlyInfinite);

                _view.SetResourceIcon(iconToSet);
                _view.SetInfiniteStatus(isCurrentlyInfinite);
                if (isCurrentlyInfinite)
                {
                    TimeSpan remaining = _infiniteStatus.GetRemainingInfiniteTime(ResourceHash);
                    _view.UpdateInfinityRemainingTime(TimeFormatter.FormatRemainingTime(remaining));
                }

                if (!isCurrentlyInfinite)
                {
                    string exactKey = _currentResourceData != null ? _currentResourceData.ResourceId : ResourceKey;
                    long displayAmount = _converter.ToLong(_model.GetAmount(exactKey));
                    _view.SetResourceAmount(displayAmount);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1), delayTiming: PlayerLoopTiming.Update,
                    cancellationToken: token);
            }
        }

        private Sprite SetIcon(bool isCurrentlyInfinite)
        {
            _currentResourceData = _model.GetResourceData(ResourceHash);
            Sprite iconToSet = _currentResourceData?.Icon;
            if (isCurrentlyInfinite && _currentResourceData != null && _currentResourceData.InfinityIcon != null)
            {
                iconToSet = _currentResourceData.InfinityIcon;
            }

            return iconToSet;
        }

        private void SubscribeToEvents()
        {
            // Sử dụng Action thuần của C# thay thế R3/IEventService
            _model.OnResourceChanged += OnResourceChanged;
            _model.OnResourceInsufficient += OnResourceInsufficient;
        }

        private void OnResourceInsufficient(int hash)
        {
            if (hash == ResourceHash)
            {
                _view.ShowInsufficientMessage();
            }
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
                    _view.SetResourceAmountWithoutAnimation(newDisplay);
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
                    bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceHash);
                    Sprite iconToSet = SetIcon(isCurrentlyInfinite);
                    _view.SetResourceIcon(iconToSet);
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
            long displayAmount = _converter.ToLong(_model.GetAmount(_currentResourceData.ResourceId));
            _view.SetResourceAmountWithoutAnimation(displayAmount);
            bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceHash);
            Sprite iconToSet = SetIcon(isCurrentlyInfinite);
            _view.SetResourceIcon(iconToSet);
            _view.SetResourceName(_currentResourceData.DisplayName);
        }

        private void UpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceHash);
            if (_currentResourceData == null) return;
            bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceHash);
            Sprite iconToSet = SetIcon(isCurrentlyInfinite);
            _view.SetResourceIcon(iconToSet);
            long displayAmount = _converter.ToLong(_model.GetAmount(_currentResourceData.ResourceId));
            _view.SetResourceAmount(displayAmount);
            _view.SetResourceName(_currentResourceData.DisplayName);
        }
      

        public void Cleanup()
        {
            // CRITICAL: Phải hủy đăng ký sự kiện khi không dùng nữa để tránh Memory Leak
            if (_model != null)
            {
                _model.OnResourceChanged -= OnResourceChanged;
                _model.OnResourceInsufficient -= OnResourceInsufficient;
            }

            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _updateQueue.Clear();
        }
    }
}