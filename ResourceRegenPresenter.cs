using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourceRegenPresenter : IDisposable
    {
        private readonly IResourceRegenView _view;
      
        private readonly string _resourceKey;
        private readonly int _resourceHash;
        
        private readonly ResourceManager _resourceManager; // Thay đổi từ IResourceService sang ResourceManager để lấy Model trực tiếp
        private CancellationTokenSource _cts;

        public ResourceRegenPresenter(
            IResourceRegenView view, 
            string resourceKey,
            ResourceManager resourceManager)
        {
            _view = view;
            _resourceKey = resourceKey;
            _resourceHash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _resourceManager = resourceManager;
        }

        public void Initialize()
        {
            // Đăng ký trực tiếp vào Action của LongModel
            if (_resourceManager?.LongModel != null)
            {
                _resourceManager.LongModel.OnResourceChanged += OnResourceChanged;
            }
         
            _cts = new CancellationTokenSource();
            UpdateVisualLoopAsync(_cts.Token).Forget();
            UpdateVisuals();
        }

        private void OnResourceChanged(ResourceChangeData<long> changeData)
        {
            if (changeData.ResourceId == _resourceHash)
            {
                UpdateVisuals();
            }
        }

        private async UniTaskVoid UpdateVisualLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                UpdateVisuals();
                await UniTask.Delay(TimeSpan.FromSeconds(1), delayTiming: PlayerLoopTiming.Update, cancellationToken: token);
            }
        }

        private void UpdateVisuals()
        {
            if (_view == null || _resourceManager == null) return;
           
            if (_resourceManager.IsCurrentlyInfinite(_resourceKey))
            {
                _view.SetRegenStatusActive(false);
                return;
            }
           
            if (_resourceManager.IsAtMaxStack(_resourceKey))
            {
                _view.SetRegenStatusActive(true);
                _view.SetRegenStatusText("Full");
                return;
            }
        
            if (_resourceManager.IsRegenEnabled(_resourceKey))
            {
                DateTime nextRegenTime = _resourceManager.GetNextRegenTime(_resourceKey);
                TimeSpan remainingTime = nextRegenTime - DateTime.UtcNow;
                if (remainingTime < TimeSpan.Zero) remainingTime = TimeSpan.Zero;
                _view.SetRegenStatusActive(true);
                _view.SetRegenStatusText(TimeFormatter.FormatRemainingTime(remainingTime));
            }
            else
            {
                _view.SetRegenStatusActive(false);
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            
            // Hủy đăng ký Action khi hủy đối tượng
            if (_resourceManager?.LongModel != null)
            {
                _resourceManager.LongModel.OnResourceChanged -= OnResourceChanged;
            }
        }
    }
}