using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IInternetTimeService
    {
        DateTime InternetTimeUtc { get; }
        bool IsInitialized { get; }
        bool IsTimeValid { get; }
        event Action<bool> OnTimeValidityChanged;
        UniTask<bool> SyncInternetTimeAsync(CancellationToken cancellationToken);
        void OnNetworkStatusChanged(bool isOnline);
        DateTime GetCurrentTime();
        bool IsTimePassed(DateTime targetTimeUtc);
        TimeSpan GetTimeUntil(DateTime targetTimeUtc);
    }
}
