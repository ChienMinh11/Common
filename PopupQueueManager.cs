using System;
using System.Collections.Generic;
using System.Threading;
using ChieChie.Constracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public class PopupQueueRequest
    {
        public string PopupNameId { get; }
        public string Message { get; }
        public int Priority { get; }
        public bool CloseAndRestore { get; }

        public PopupQueueRequest(string popupNameId, string message = "", int priority = 0,
            bool closeAndRestore = false)
        {
            PopupNameId = popupNameId;
            Message = message;
            Priority = priority;
            CloseAndRestore = closeAndRestore;
        }
    }

    public interface IPopupQueueService
    {
        void Enqueue(PopupQueueRequest request);
        void EnqueueMultiple(IEnumerable<PopupQueueRequest> requests);
        void ClearQueue();
        void CancelQueue();
    }

    public class PopupQueueManager : IPopupQueueService, IDisposable
    {
        private readonly IPopupService _popupService;
        private readonly List<PopupQueueRequest> _queue = new List<PopupQueueRequest>();

        private bool _isProcessing;
        private CancellationTokenSource _queueCts;

        public PopupQueueManager(IPopupService popupService)
        {
            _popupService = popupService;

            // Đăng ký Action thay thế cho _eventService.Observe
            _popupService.OnPopupClosedAction += OnPopupClosed;
        }

        public void Enqueue(PopupQueueRequest request)
        {
            _queue.Add(request);
            _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            if (!_isProcessing)
            {
                TriggerProcessQueue();
            }
        }

        public void EnqueueMultiple(IEnumerable<PopupQueueRequest> requests)
        {
            foreach (var req in requests)
            {
                _queue.Add(req);
            }

            _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            if (!_isProcessing)
            {
                TriggerProcessQueue();
            }
        }

        private void OnPopupClosed(IPopup closedPopup)
        {
            if (_queueCts == null || _queueCts.IsCancellationRequested) return;

            if (!_popupService.HasActivePopups() && _queue.Count > 0)
            {
                _isProcessing = true;
                ExecuteNextPopupWithDelayAsync().Forget();
            }
            else if (_queue.Count == 0)
            {
                _isProcessing = false;
            }
        }
        
        private async UniTaskVoid ExecuteNextPopupWithDelayAsync()
        {
           
            await UniTask.Yield(PlayerLoopTiming.Update, _queueCts.Token);
            if (!_popupService.HasActivePopups() && _queue.Count > 0 && _queueCts != null && !_queueCts.IsCancellationRequested)
            {
                ProcessNextPopupAsync(_queueCts.Token).Forget();
            }
        }

        private void TriggerProcessQueue()
        {
            _isProcessing = true;
            if (_queueCts == null || _queueCts.IsCancellationRequested)
            {
                _queueCts = new CancellationTokenSource();
            }

            ProcessNextPopupAsync(_queueCts.Token).Forget();
        }

        private async UniTask ProcessNextPopupAsync(CancellationToken cancellationToken)
        {
            if (_popupService.HasActivePopups()) return;

            while (_queue.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    ResetProcessingState();
                    return;
                }

                _isProcessing = true;

                var nextRequest = _queue[0];
                _queue.RemoveAt(0);
               
                var targetPopup = await _popupService.PreloadPopup(nextRequest.PopupNameId)
                    .AttachExternalCancellation(cancellationToken);

                if (targetPopup != null)
                {
                    if (!targetPopup.CanAutoShow())
                    {
                        if (!targetPopup.IsCacheable) _popupService.ReleaseUnusedPopup(nextRequest.PopupNameId);
                        continue; 
                    }
                }

                try
                {
                    bool showSuccess = await _popupService.ShowPopup(
                        nextRequest.PopupNameId,
                        nextRequest.Message,
                        nextRequest.CloseAndRestore
                    ).AttachExternalCancellation(cancellationToken);

                    if (showSuccess)
                    {
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    ResetProcessingState();
                    return;
                }
            }

            _isProcessing = false;
        }

        public void CancelQueue()
        {
            Debug.Log("[PopupQueue] Đã hủy bỏ toàn bộ chuỗi Popup đang chờ.");

            if (_queueCts != null)
            {
                _queueCts.Cancel();
                _queueCts.Dispose();
                _queueCts = null;
            }

            _queue.Clear();
            _isProcessing = false;
        }

        public void ClearQueue()
        {
            _queue.Clear();
            if (!_isProcessing)
            {
                _isProcessing = false;
            }
        }

        private void ResetProcessingState()
        {
            _isProcessing = false;
        }

        public void Dispose()
        {
            CancelQueue();
            if (_popupService != null)
            {
                _popupService.OnPopupClosedAction -= OnPopupClosed;
            }
        }
    }
}