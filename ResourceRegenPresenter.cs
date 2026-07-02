using System;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;

namespace ChieChie.Resource
{
    public class ResourceRegenPresenter : IDisposable
    {
        private readonly IResourceView _view;
        private readonly string _resourceKey;
        private readonly ResourceManager _resourceManager; 
        private CancellationTokenSource _cts;
        public ResourceRegenPresenter(
            IResourceView view, 
            string resourceKey,
            ResourceManager resourceManager)
        {
            _view = view;
            _resourceKey = resourceKey;
            _resourceManager = resourceManager;
        }

        public void Initialize()
        {
            if (_resourceManager != null)
            {
                if (_resourceManager.LongModel != null)
                {
                    _resourceManager.LongModel.OnResourceChanged += OnResourceChanged;
                }
                
                // ĐĂNG KÝ THÊM SỰ KIỆN VÔ HẠN Ở ĐÂY
                _resourceManager.OnInfiniteAdded += OnInfiniteStatusChanged;
                _resourceManager.OnInfiniteExpired += OnInfiniteStatusExpired;
                _resourceManager.OnResourcePendingUpdatesProcessed += OnPendingUpdatesProcessed;
            }
         
            UpdateVisuals();
        }

        private void OnResourceChanged(ResourceChangeData<long> changeData)
        {
            if (changeData.ResourceId == _resourceKey)
            {
                UpdateVisuals();
            }
        }
        private void OnInfiniteStatusChanged(string resourceKey, bool delayUpdate)
        {
            if (resourceKey == _resourceKey)
            {
                UpdateVisuals();
            }
        }

        private void OnPendingUpdatesProcessed(string resourceKey)
        {
            if (resourceKey == _resourceKey)
            {
                UpdateVisuals();
            }
        }

        private void OnInfiniteStatusExpired(string resourceKey)
        {
            if (resourceKey == _resourceKey)
            {
                UpdateVisuals();
            }
        }
    

        private void UpdateVisuals()
        {
            if (_view == null || _resourceManager == null) return;
          
            bool isInfiniteDelayed = _resourceManager.IsInfiniteDisplayDelayed(_resourceKey);

            if (_resourceManager.IsCurrentlyInfinite(_resourceKey) && !isInfiniteDelayed)
            {
                _view.UpdateRegenStatus(false, false, DateTime.MinValue);
                return;
            }
   
            bool isMaxStack = _resourceManager.IsAtMaxStack(_resourceKey);
            bool isRegenEnabled = _resourceManager.IsRegenEnabled(_resourceKey);
            DateTime nextRegenTime = _resourceManager.GetNextRegenTime(_resourceKey);

            _view.UpdateRegenStatus(isRegenEnabled, isMaxStack, nextRegenTime);
        }

        public void Dispose()
        {
            if (_resourceManager != null)
            {
                if (_resourceManager.LongModel != null)
                {
                    _resourceManager.LongModel.OnResourceChanged -= OnResourceChanged;
                }
        
                _resourceManager.OnInfiniteAdded -= OnInfiniteStatusChanged;
                _resourceManager.OnInfiniteExpired -= OnInfiniteStatusExpired;
                _resourceManager.OnResourcePendingUpdatesProcessed -= OnPendingUpdatesProcessed;
            }
        }
    }
}