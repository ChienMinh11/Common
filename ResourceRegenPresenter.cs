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
            if (changeData.ResourceId == _resourceKey)
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
          
            bool isInfiniteDelayed = _resourceManager.IsInfiniteDisplayDelayed(_resourceKey);

            if (_resourceManager.IsCurrentlyInfinite(_resourceKey) && !isInfiniteDelayed)
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
            
            if (_resourceManager?.LongModel != null)
            {
                _resourceManager.LongModel.OnResourceChanged -= OnResourceChanged;
            }
        }
    }
}