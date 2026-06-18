using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ChieChie.Booster
{
    public interface IBoosterService
    {
        UniTask<bool> InitializeAsync(CancellationToken cancellationToken);
        UniTask<bool> UseBooster(string boosterType, CancellationToken cancellationToken = default);
        event Action<string?> OnAwaitingStatusChanged;
        event Action<string> OnPreBoosterStateChanged;
        event Action<string> OnBoosterInfinitePassConsumed;
        void ResetBooster(string boosterType);
        BoosterBehavior GetBoosterBehavior(string powerUpType);
        UniTask<bool> ActivateAllSelectedPreBoosters(CancellationToken cancellationToken = default);
        void CleanUp();
    }
}
