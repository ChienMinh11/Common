using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ChieChie.Core
{
    public enum NetworkEventType
    {
        NetworkStatusChanged
    }
    public class NetworkCheckerStatus : MonoBehaviour, IServiceInitialisable
    {
        private const float NETWORK_CHECK_INTERVAL = 0.5f;
      
        private bool _isOnline = true;
        public bool IsOnline => _isOnline;
        private IEventService _eventService;
        private bool _isInitialized = false;
        
        public int InitializationPriority => 0;
        public bool IsInitialized => _isInitialized;
     

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                _isInitialized = true;
                return UniTask.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize NetworkCheckerStatus: {ex.Message}");
                return UniTask.FromResult(true);
            }
        }
        
        [Button]
        public async void CheckNetworkStatus(IEventService eventService)
        {
            bool wasOnline = _isOnline;
            _isOnline = await NetworkChecker.CheckNetworkConnectivityReliable(this.GetCancellationTokenOnDestroy());
            if (wasOnline != _isOnline)
            {
                eventService.Publish(NetworkEventType.NetworkStatusChanged, _isOnline);
            }
        }
        
        public async UniTaskVoid StartNetworkChecking(IEventService eventService)
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    CheckNetworkStatus(eventService);
                    await UniTask.Delay(TimeSpan.FromSeconds(NETWORK_CHECK_INTERVAL), cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }
    }
}