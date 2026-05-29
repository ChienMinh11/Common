using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3; 
using UnityEngine;

namespace ChieChie.Core
{
    public class ResourceRegenPresenter : IDisposable
    {
        private readonly IResourceRegenView _view;
        private readonly ResourceType _resourceType;
        private readonly IResourceManager _resourceManager;
        private readonly IResourceRegenService _regenService;
        private readonly IEventService _eventService;
        
        private CancellationTokenSource _cts;
        private ResourceRegenData _regenConfigData;
    
        private readonly CompositeDisposable _disposableBag = new CompositeDisposable();

        public ResourceRegenPresenter(
            IResourceRegenView view, 
            ResourceType resourceType,
            IResourceManager resourceManager,
            IResourceRegenService regenService,
            IEventService eventService,
            ResourceRegenConfig regenConfig)
        {
            _view = view;
            _resourceType = resourceType;
            _resourceManager = resourceManager;
            _regenService = regenService;
            _eventService = eventService;
            
            if (regenConfig != null)
            {
                _regenConfigData = regenConfig.GetRegenData(resourceType);
            }
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
            if (changeData.ResourceId == _resourceType)
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
            if (_view == null || _view.StatusText == null) return;
          
            if (_resourceManager.IsCurrentlyInfinite(_resourceType))
            {
                if(_view.StatusText!=null) _view.StatusText.gameObject.SetActive(false);
                return;
            }
          
            if (_resourceManager.IsAtMaxStack(_resourceType))
            {
                if (_view.StatusText != null)
                {
                    _view.StatusText.gameObject.SetActive(true);
                    _view.StatusText.text = "Full";
                }
               
                return;
            }
           
            if (_regenService.IsRegenEnabled(_resourceType) && _regenConfigData != null)
            {
                if(_view.StatusText!=null) _view.StatusText.gameObject.SetActive(true);
                float currentTimer = _regenService.GetCurrentTimer(_resourceType); 
                float remainingRegenSeconds = Mathf.Max(0, _regenConfigData.intervalSeconds - currentTimer);
                
               
                if (_view.StatusText != null)
                {
                    _view.StatusText.gameObject.SetActive(true);
                    _view.StatusText.text = CoreExtensions.FormatRemainingTime(TimeSpan.FromSeconds(remainingRegenSeconds));
                }
              
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