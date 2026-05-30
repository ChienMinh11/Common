using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IInitialisable 
    {
        UniTask<bool> InitializeAsync(CancellationToken cancellationToken);
        bool IsInitialized { get; }
    }
}
