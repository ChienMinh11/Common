using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
    public class ShopManager : MonoBehaviour, IInitialisable
    {
        [SerializeField] private ShopConfig config;
        public ShopPresenter Presenter { get; private set; }

        private ShopModel _shopModel;
        private IIapBrigde _iapBridge;
        private SaveSystem _saveSystem;
        private IResourceManager _resourceManager;

        [Inject]
        public void Construct(IIapBrigde iapBridge, SaveSystem saveSystem, IResourceManager resourceManager)
        {
            _iapBridge = iapBridge;
            _saveSystem = saveSystem;
            _resourceManager = resourceManager;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            _shopModel = new ShopModel(_iapBridge, _saveSystem, config, _resourceManager);
            Presenter = new ShopPresenter(_shopModel,_resourceManager.GetConfig());
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        public bool IsInitialized { get; set; }

        private void OnDestroy()
        {
            Presenter?.Dispose();
        }
    }
}