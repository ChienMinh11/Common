using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class InternetTimeService : IInternetTimeService, IAsyncStartable, IDisposable
    {
        private readonly TimeServiceSettings _settings;
        
        private DateTime _fetchedInternetTimeUtc;
        private double _unscaledTimeAtSync; 
        private int _serverIndex;
        private readonly CancellationTokenSource _syncCancellationTokenSource;

        private readonly string[] timeApiUrls =
        {
            "https://www.google.com",
            "https://www.cloudflare.com",
            "https://www.microsoft.com"
        };

        public bool IsInitialized { get; private set; }
        public bool IsTimeValid { get; private set; }
        public event Action<bool> OnTimeValidityChanged;

        // Constructor Injection nhận Settings từ VContainer
        [Inject]
        public InternetTimeService(TimeServiceSettings settings)
        {
            _settings = settings;
            _syncCancellationTokenSource = new CancellationTokenSource();
        }

        public DateTime InternetTimeUtc
        {
            get
            {
                if (_settings.useLocalTimeForTesting) return DateTime.UtcNow;
                if (!IsTimeValid) return DateTime.UtcNow;

                double elapsedSeconds = Time.realtimeSinceStartupAsDouble - _unscaledTimeAtSync;
                return _fetchedInternetTimeUtc.AddSeconds(elapsedSeconds);
            }
        }
      
        public  UniTask StartAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[InternetTimeService] Initializing via VContainer...");

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            _settings.useLocalTimeForTesting = false;
#else
            if (_settings.useLocalTimeForTesting)
            {
                Debug.LogWarning("[InternetTimeService] TEST MODE ACTIVE - Using local Utc time");
                IsTimeValid = true;
                _fetchedInternetTimeUtc = DateTime.UtcNow;
                _unscaledTimeAtSync = Time.realtimeSinceStartupAsDouble;
                IsInitialized = true;
                return UniTask.CompletedTask;
            }
#endif

            IsInitialized = true;
            
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _syncCancellationTokenSource.Token).Token;
            
            SyncInternetTimeAsync(linkedToken).Forget();
            PeriodicTimeSync(_syncCancellationTokenSource.Token).Forget();

            Debug.Log("[InternetTimeService] Initialized successfully.");
            
            return UniTask.CompletedTask;
        }

        public async UniTask<bool> SyncInternetTimeAsync(CancellationToken cancellationToken)
        {
            var success = false;
            _serverIndex = 0;

            while (_serverIndex < timeApiUrls.Length && !success && !cancellationToken.IsCancellationRequested)
            {
                success = await TryGetTimeFromAPIAsync(timeApiUrls[_serverIndex], cancellationToken);

                if (!success)
                {
                    _serverIndex++;
                    if (_settings.debugMode)
                        Debug.Log($"[InternetTimeService] Server failed, trying next server ({_serverIndex}/{timeApiUrls.Length})");

                    await UniTask.Delay(500, cancellationToken: cancellationToken);
                }
            }

            var previousValidity = IsTimeValid;
            IsTimeValid = success;

            if (previousValidity != IsTimeValid)
                OnTimeValidityChanged?.Invoke(IsTimeValid);

            if (success && _settings.debugMode)
            {
                Debug.Log($"[InternetTimeService] Time synced successfully. UTC Internet time: {InternetTimeUtc}");
            }
            else if (!success)
            {
                Debug.LogWarning("[InternetTimeService] Failed to sync with any time server.");
            }

            return success;
        }

        public void OnNetworkStatusChanged(bool isOnline)
        {
            if (isOnline && !IsTimeValid)
            {
                Debug.Log("[InternetTimeService] Network restored, syncing time...");
                SyncInternetTimeAsync(_syncCancellationTokenSource.Token).Forget();
            }
        }

        private async UniTask<bool> TryGetTimeFromAPIAsync(string apiUrl, CancellationToken cancellationToken)
        {
            try
            {
                using (var request = UnityWebRequest.Get(apiUrl))
                {
                    request.timeout = _settings.serverTimeoutSeconds;
                    await request.SendWebRequest().WithCancellation(cancellationToken);

                    if (request.result != UnityWebRequest.Result.Success) return false;
                 
                    var serverTime = DateTime.UtcNow;
                    var timeFound = false;
                    var dateHeader = request.GetResponseHeader("Date");
                    
                    if (!string.IsNullOrEmpty(dateHeader) && DateTime.TryParse(dateHeader, out serverTime))
                    {
                        timeFound = true;
                    }

                    if (timeFound)
                    {
                        _fetchedInternetTimeUtc = serverTime.ToUniversalTime();
                        _unscaledTimeAtSync = Time.realtimeSinceStartupAsDouble;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Ignored in production
            }
            return false;
        }

        private async UniTaskVoid PeriodicTimeSync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromMinutes(_settings.syncIntervalMinutes), cancellationToken: cancellationToken);
                if (!cancellationToken.IsCancellationRequested) 
                    await SyncInternetTimeAsync(cancellationToken);
            }
        }

        public DateTime GetCurrentTime() => InternetTimeUtc;
        public bool IsTimePassed(DateTime targetTimeUtc) => InternetTimeUtc >= targetTimeUtc;
        public TimeSpan GetTimeUntil(DateTime targetTimeUtc) => targetTimeUtc - InternetTimeUtc;

        // Thay thế OnDestroy của MonoBehaviour
        public void Dispose()
        {
            _syncCancellationTokenSource?.Cancel();
            _syncCancellationTokenSource?.Dispose();
        }
    }
}