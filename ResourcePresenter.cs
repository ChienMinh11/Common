using System;
using ChieChie.Constracts;
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

        public string ResourceKey { get; }
        public bool HasPendingUpdates => _updateQueue.HasPendingUpdates;
        
        private bool _isInfiniteUpdateDelayed = false;
        private bool _isCurrentlyShowingInfinite = false; 
        private bool _isInfiniteTimeUpdateDelayed = false;
        public bool IsInfiniteUpdateDelayed => _isInfiniteUpdateDelayed;
        
        public event Action<string> OnPendingUpdatesProcessed;

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
            _updateQueue = new ResourceUpdateQueue();
            
            SubscribeToEvents();
            UpdateView();
        }

        private void SubscribeToEvents()
        {
            if (_model != null)
            {
                _model.OnResourceChanged += OnResourceChanged;
                _model.OnResourceInsufficient += OnResourceInsufficient;
            }
            if (_infiniteStatus != null)
            {
                _infiniteStatus.OnInfiniteAdded += OnInfiniteAddedFromManager;
            }
        }

        private void OnResourceInsufficient(string resourceKey)
        {
            if (resourceKey == ResourceKey)
            {
                _view?.ShowInsufficientMessage();
            }
        }

        private void OnResourceChanged(ResourceChangeData<T> changeData)
        {
            if (changeData.ResourceId != ResourceKey) return;

            long oldDisplay = _converter.ToLong(changeData.OldAmount);
            long newDisplay = _converter.ToLong(changeData.NewAmount);

            if (changeData.DelayUpdate)
            {
                _updateQueue.EnqueueUpdate(oldDisplay, newDisplay);
            }
            else
            {
                _updateQueue.Clear();

                // Chỉ cập nhật trực tiếp số lượng nếu tài nguyên không ở trạng thái vô hạn
                if (!_infiniteStatus.IsCurrentlyInfinite(ResourceKey))
                {
                    _view?.SetResourceAmountWithoutAnimation(newDisplay);
                }
                
                // Cập nhật lại toàn bộ mốc thời gian tĩnh (nếu có) khi số lượng thay đổi
                UpdateView();
            }
        }

        private void OnInfiniteAddedFromManager(string resourceKey, bool delayUpdate)
        {
            if (resourceKey != ResourceKey) return;

            if (delayUpdate)
            {
                long currentAmount = _converter.ToLong(_model.GetAmount(ResourceKey));
                
                if (_isCurrentlyShowingInfinite)
                {
                    _isInfiniteTimeUpdateDelayed = true;
                    _updateQueue.EnqueueUpdate(currentAmount, currentAmount); 
                }
                else
                {
                    _isInfiniteUpdateDelayed = true;
                    _updateQueue.EnqueueUpdate(0, currentAmount);
                }
            }
            else
            {
                _isInfiniteUpdateDelayed = false;
                _isInfiniteTimeUpdateDelayed = false;
                UpdateView();
            }
        }

        public void ProcessPendingUpdates()
        {
            _isInfiniteUpdateDelayed = false;
            _isInfiniteTimeUpdateDelayed = false;

            var update = _updateQueue.ProcessNextUpdate(ResourceKey);
            long finalAmount = update != null ? update.Amount : _converter.ToLong(_model.GetAmount(ResourceKey));
            _currentResourceData = _model.GetResourceData(ResourceKey);
            if (_currentResourceData != null && _view != null)
            {
                bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceKey);
                _isCurrentlyShowingInfinite = isCurrentlyInfinite;

                if (!isCurrentlyInfinite)
                {
                    _view.SetResourceAmount(finalAmount);
                }
        
                Sprite iconToSet = SetIcon(isCurrentlyInfinite);
                _view.SetResourceIcon(iconToSet);
                _view.SetResourceName(_currentResourceData.DisplayName);

                DateTime expireTime = isCurrentlyInfinite 
                    ? DateTime.UtcNow.Add(_infiniteStatus.GetRemainingInfiniteTime(ResourceKey)) 
                    : DateTime.MinValue;
                _view.UpdateInfinityStatus(isCurrentlyInfinite, expireTime);

                if (_currentResourceData.MaxStack > 0 && finalAmount >= _currentResourceData.MaxStack)
                {
                    _view.OnMaxStackReached(ResourceKey);
                }
            }

            OnPendingUpdatesProcessed?.Invoke(ResourceKey);
        }
        
        public void ForceUpdateView()
        {
            UpdateView();
        }

        private void UpdateView()
        {
            _currentResourceData = _model.GetResourceData(ResourceKey);
            if (_currentResourceData == null || _view == null) return;
            bool isCurrentlyInfinite = _infiniteStatus.IsCurrentlyInfinite(ResourceKey) && !_isInfiniteUpdateDelayed;
            _isCurrentlyShowingInfinite = isCurrentlyInfinite;

            Sprite iconToSet = SetIcon(isCurrentlyInfinite);
            _view.SetResourceIcon(iconToSet);
            _view.SetResourceName(_currentResourceData.DisplayName);

            long displayAmount = _converter.ToLong(_model.GetAmount(_currentResourceData.ResourceId));
            _view.SetResourceAmount(displayAmount);
            DateTime expireTime = isCurrentlyInfinite 
                ? DateTime.UtcNow.Add(_infiniteStatus.GetRemainingInfiniteTime(ResourceKey)) 
                : DateTime.MinValue;
            
            _view.UpdateInfinityStatus(isCurrentlyInfinite, expireTime);
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

        public void Cleanup()
        {
            if (_model != null)
            {
                _model.OnResourceChanged -= OnResourceChanged;
                _model.OnResourceInsufficient -= OnResourceInsufficient;
            }
            if (_infiniteStatus != null)
            {
                _infiniteStatus.OnInfiniteAdded -= OnInfiniteAddedFromManager;
            }
            _updateQueue.Clear();
        }
    }
}