using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public class ShopManager : MonoBehaviour,IInitialisable,IShopService
    {
        [SerializeField] ShopConfig config;
        
        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            return UniTask.FromResult(true);
        }

        public bool IsInitialized { get; set; }
    }
}
