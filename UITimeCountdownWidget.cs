using System;
using System.Threading;
using ChieChie.Constracts; // Thêm namespace chứa ITimeProvider nếu cần
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class UITimeCountdownWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtRemainingTime;

        private CancellationTokenSource _countdownCts;
        private DateTime _targetEndTime;
        private string _lastTimeStr = string.Empty;
        private ITimeProvider _timeProvider;
        private Action _onCompleted;
        private bool _hasCompleted;

        [Inject]
        private void Construct(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public void Setup(DateTime endTime, Action onCompleted = null)
        {
            _targetEndTime = endTime;
            _onCompleted = onCompleted;
            _hasCompleted = false;
            if (gameObject.activeInHierarchy)
            {
                StartCountdown();
            }
        }
        
        public void Setup(TimeSpan remainingTime, Action onCompleted = null)
        {
            var now = _timeProvider != null ? _timeProvider.UtcNow : DateTime.UtcNow;
            _targetEndTime = now + remainingTime;
            _onCompleted = onCompleted;
            _hasCompleted = false;
    
            if (gameObject.activeInHierarchy)
            {
                StartCountdown();
            }
        }

        private void OnEnable()
        {
            if (_targetEndTime != default)
            {
                StartCountdown();
            }
        }

        private void OnDisable()
        {
            StopCountdown();
        }

        private void OnDestroy()
        {
            StopCountdown();
        }

        private void StartCountdown()
        {
            StopCountdown();
            _countdownCts = new CancellationTokenSource();
            CountdownLoop(_countdownCts.Token).Forget();
        }

        private void StopCountdown()
        {
            if (_countdownCts != null)
            {
                _countdownCts.Cancel();
                _countdownCts.Dispose();
                _countdownCts = null;
            }
            _lastTimeStr = string.Empty;
        }

        private async UniTaskVoid CountdownLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var now = _timeProvider != null ? _timeProvider.UtcNow : DateTime.UtcNow;

                    if (now >= _targetEndTime)
                    {
                        UpdateTextTime("Event End!");
                        NotifyCompleted();
                        break;
                    }

                    TimeSpan remaining = _targetEndTime - now;
                    float delaySeconds = 1f;
                    if (remaining.TotalDays >= 1)
                    {
                        UpdateTextTime($"{Math.Floor(remaining.TotalDays):00}d:{(int)remaining.Hours:00}h");
                        delaySeconds = 60f;
                    }
                    else if (remaining.TotalHours >= 1)
                    {
                        UpdateTextTime($"{Math.Floor(remaining.TotalHours):00}h:{(int)remaining.Minutes:00}");
                        delaySeconds = 10f;
                    }
                    else
                    {
                        UpdateTextTime($"{(int)remaining.Minutes:00}:{(int)remaining.Seconds:00}");
                        delaySeconds = 1f;
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), delayType: DelayType.Realtime, cancellationToken: token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
        }

        private void NotifyCompleted()
        {
            if (_hasCompleted) return;
            _hasCompleted = true;
            _onCompleted?.Invoke();
        }

        private void UpdateTextTime(string timeStr)
        {
            if (txtRemainingTime == null) return;
            if (_lastTimeStr == timeStr) return;
            _lastTimeStr = timeStr;
            txtRemainingTime.text = timeStr;
        }
    }
}
