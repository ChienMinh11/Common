using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface ITransitionScreen 
    {
        bool IsInitialized { get; }
        UniTask PlayTransitionInAsync();
        UniTask PlayTransitionOutAsync(float delaySeconds = -1f);
    }
}
