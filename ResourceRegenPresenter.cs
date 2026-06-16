using System;
using System.Threading;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine; // Cần thêm để sử dụng Animator.StringToHash

namespace ChieChie.Resource
{
    public class ResourceRegenPresenter : IDisposable
    {
        private readonly IResourceRegenView _view;
        
        // SỬA: Chuyển đổi định danh quản lý từ ResourceType sang chuỗi Key và mã Hash số nguyên
        private readonly string _resourceKey;
        private readonly int _resourceHash;
        
        private readonly IResourceService _resourceService; 
        private readonly IEventService _eventService;
        
        private CancellationTokenSource _cts;
        private readonly CompositeDisposable _disposableBag = new CompositeDisposable();

        public ResourceRegenPresenter(
            IResourceRegenView view, 
            string resourceKey, // SỬA: Nhận string thay vì ResourceType enum
            IResourceService resourceService,
            IEventService eventService)
        {
            _view = view;
            _resourceKey = resourceKey;
            // Tự động băm chuỗi định danh để xử lý logic tìm kiếm và bắt Event nội bộ nhanh
            _resourceHash = string.IsNullOrEmpty(resourceKey) ? 0 : Animator.StringToHash(resourceKey);
            _resourceService = resourceService;
            _eventService = eventService;
        }

        public void Initialize()
        {
            // Lắng nghe dữ liệu thay đổi từ Model thông qua ResourceChangeData nhận dạng bằng Hash int
            _eventService.Observe<ResourceChangeData<long>, ResourceEventType>(ResourceEventType.ResourceChanged)
                .Subscribe(OnResourceChanged)
                .AddTo(_disposableBag); 
         
            _cts = new CancellationTokenSource();
            UpdateVisualLoopAsync(_cts.Token).Forget();
            UpdateVisuals();
        }

        private void OnResourceChanged(ResourceChangeData<long> changeData)
        {
            // SỬA: Kiểm tra trùng khớp định danh bằng mã Hash (int)
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
          
            // SỬA: Các hàm API của IResourceService đã được chuyển sang nhận diện bằng string hoặc mã Hash int
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