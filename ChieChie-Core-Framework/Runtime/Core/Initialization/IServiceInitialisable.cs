using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IServiceInitialisable 
    {
        UniTask<bool> InitializeAsync(CancellationToken cancellationToken);
        bool IsInitialized { get; }
    }
   
}
