using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MyFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace GameCore.Runtime
{
    public class ResourceManager : MonoBehaviour, IResourceManager, IInitialisable
    {
        [SerializeField] private ResourceConfig resourceConfig;
        [SerializeField] private bool useLongNumbers = false;

        private ResourceModel<int> _intModel;
        private ResourceModel<long> _longModel;
        
        private ResourcePresenterFactory _factory;
        private IEventService _eventService;
        private ISaveSystem _saveSystem;

        private readonly List<IResourcePresenter> _activePresenters = new();

        public int InitializationPriority => 0; 
        public bool IsInitialized { get; private set; }

        [Inject]
        private void Construct(IEventService eventService, ISaveSystem saveSystem)
        {
            _eventService = eventService;
            _saveSystem = saveSystem;
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_eventService == null)
                {
                    Debug.LogError("[ResourceManager] Failed to get IEventService");
                    return UniTask.FromResult(false);
                }

                _factory = new ResourcePresenterFactory(_eventService);

                if (useLongNumbers)
                {
                    _longModel = new ResourceModel<long>(new LongConverter(), _eventService, _saveSystem);
                    _longModel.Initialize(resourceConfig);
                    _longModel.InitializeDefaultValues();
                }
                else
                {
                    _intModel = new ResourceModel<int>(new IntConverter(), _eventService, _saveSystem);
                    _intModel.Initialize(resourceConfig);
                    _intModel.InitializeDefaultValues();
                }

                IsInitialized = true;
                return UniTask.FromResult(false); // Trả về true nếu thành công tùy logic project của bạn
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize ResourceManager: {e.Message}");
                return UniTask.FromResult(false);
            }
        }

        // Hiện thực từ IResourceManager
        public IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view)
        {
            if (!IsInitialized) return null;

            object activeModel = useLongNumbers ? (object)_longModel : (object)_intModel;
            var presenter = _factory.CreatePresenter(resourceType, view, activeModel, useLongNumbers);
            
            if (presenter != null)
            {
                _activePresenters.Add(presenter);
            }

            return presenter; 
        }

        // Hiện thực từ IResourceManager
        public void UnregisterPresenter(IResourcePresenter presenter)
        {
            if (presenter == null) return;
            
            if (_activePresenters.Contains(presenter))
            {
                presenter.Cleanup();
                _activePresenters.Remove(presenter);
            }
        }

        #region Gameplay API
        [Button]
        public void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false)
        {
            if (useLongNumbers) _longModel?.AddResource(resourceType, amount, delayUpdate);
            else _intModel?.AddResource(resourceType, (int)amount, delayUpdate);
        }

        [Button]
        public bool SpendResource(ResourceType resourceType, long amount)
        {
            if (useLongNumbers) return _longModel?.SpendResource(resourceType, amount) ?? false;
            return _intModel?.SpendResource(resourceType, (int)amount) ?? false;
        }

        [Button]
        public long GetCurrentAmount(ResourceType resourceType)
        {
            if (useLongNumbers) return _longModel != null ? _longModel.GetAmount(resourceType) : 0;
            return _intModel != null ? _intModel.GetAmount(resourceType) : 0;
        }

        [Button]
        public bool IsAtMaxStack(ResourceType resourceType)
        {
            var resourceData = resourceConfig.GetResourceData(resourceType);
            if (resourceData == null || resourceData.MaxStack <= 0) return false;
            return GetCurrentAmount(resourceType) >= resourceData.MaxStack;
        }
        #endregion

        private void OnDestroy()
        {
            foreach (var presenter in _activePresenters)
            {
                presenter?.Cleanup();
            }
            _activePresenters.Clear();
            _intModel?.Cleanup();
            _longModel?.Cleanup();
        }
    }
}