using System;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace ChieChie.Resource
{
    public class ResourceRegenPresenter : IDisposable
    {
        private readonly IResourceRegenView _view;
      
        private readonly string _resourceKey;
        private readonly int _resourceHash;
        
        private readonly IResourceService _resourceService; 
        private readonly IEventService _eventService;
        
        private CancellationTokenSource _cts;
        private readonly CompositeDisposable _disposableBag = new CompositeDisposable();

        public ResourceRegenPresenter(
            IResourceRegenView view, 
            string resourceKey,
            IResourceService resourceService,
            IEventService eventService)
        {
            _view = view;
            _resourceKey = resourceKey;
            _resourceHash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _resourceService = resourceService;
            _eventService = eventService;
        }

        public void Initialize()
        {
            _eventService.Observe<ResourceChangeData<long>, ResourceEventType>(ResourceEventType.ResourceChanged)
                .Subscribe(OnResourceChanged)
                .AddTo(_disposableBag); 
         
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
            if (_view == null) return;
           
            if (_resourceService.IsCurrentlyInfinite(_resourceKey))
            {
                _view.SetRegenStatusActive(false);
                return;
            }
           
            if (_resourceService.IsAtMaxStack(_resourceKey))
            {
                _view.SetRegenStatusActive(true);
                _view.SetRegenStatusText("Full");
                return;
            }
        
            if (_resourceService.IsRegenEnabled(_resourceKey))
            {
                DateTime nextRegenTime = _resourceService.GetNextRegenTime(_resourceKey);
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
            _disposableBag.Dispose();
        }
    }
}