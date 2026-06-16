using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ChieChie.Core
{
    /// <summary>
    /// Utility class for checking network connectivity
    /// </summary>
    public class NetworkChecker
    {
        private static readonly string[] pingEndpoints = new[]
        {
            "https://www.google.com",
            "https://www.apple.com",
            "https://www.amazon.com"
        };

        public static string DefaultPingEndpoint => pingEndpoints[0];

        /// <summary>
        /// Checks if the device has network connectivity
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <param name="endpointUrl">Optional URL to ping (defaults to google.com)</param>
        /// <returns>True if network is available, false otherwise</returns>
        public static async UniTask<bool> CheckNetworkConnectivity(CancellationToken cancellationToken, string endpointUrl = null)
        {
            // Simple connectivity check using Unity's Application.internetReachability
            if (Application.internetReachability == NetworkReachability.NotReachable) 
                return false;

            // More thorough check by trying to ping a reliable server
            endpointUrl ??= DefaultPingEndpoint;

            try
            {
                using var www = UnityWebRequest.Get(endpointUrl);
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        www.Abort();
                        return false;
                    }

                    await UniTask.Yield();
                }

                return www.result == UnityWebRequest.Result.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Network check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to check network connectivity using multiple endpoints if the first one fails
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <param name="timeoutSeconds">Maximum time to wait for all checks to complete</param>
        /// <returns>True if network is available on any endpoint, false otherwise</returns>
        public static async UniTask<bool> CheckNetworkConnectivityReliable(
            CancellationToken cancellationToken, 
            int timeoutSeconds = 10)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            foreach (var endpoint in pingEndpoints)
            {
                if (cts.Token.IsCancellationRequested)
                    return false;

                bool result = await CheckNetworkConnectivity(cts.Token, endpoint);
                if (result)
                    return true;
            }

            return false;
        }
    }
}