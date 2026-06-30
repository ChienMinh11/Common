using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassService 
    {
        bool IsInitialized { get; }
        PassModel Model { get; }
        PassEventScheduler Scheduler { get; }
        PassDatabase Database { get; }

        UniTask<bool> InitializeAsync(CancellationToken cancellationToken);
        void AddPoints(int amount);
        bool CanClaimReward(int tierIndex, bool isPremium);
        void ClaimReward(int tierIndex, bool isPremium);
        bool CanClaimBonus();
        void ClaimBonus();
        void BuyPremium();
    }
}
