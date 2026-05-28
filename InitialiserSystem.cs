using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace ChieChie.Core
{
    public class InitialiserSystem : SingletonBase<InitialiserSystem>
    {
        private const string INITIALISATION_CONFIG_PATH = "Config/InitialisationConfig";
        [SerializeField]private InitialisationConfig initialisationConfig;
        protected override bool PersistAcrossScenes => true; 
        
        private readonly List<List<IInitialisable>> priorityLayers = new(); 
        private readonly List<InitializationError> initializationErrors = new(); 
        public IReadOnlyList<InitializationError> InitializationErrors => initializationErrors; 
      
        private readonly List<GameObject> addressableInstances = new();
        private readonly CancellationTokenSource initializationCts = new(); 
        
        private int _totalObjectsToInitialize = 0; 
        private int _initializedObjectsCount = 0;

        public void RegisterInitialisableObject(IInitialisable obj, int layerIndex = 0) 
        {
            while (priorityLayers.Count <= layerIndex) 
            {
                priorityLayers.Add(new List<IInitialisable>()); 
            }

            if (!priorityLayers[layerIndex].Contains(obj)) 
            {
                priorityLayers[layerIndex].Add(obj); 
            }
        }

        public async UniTask<List<GameObject>> PrepareAndLoadAllLayersAsync()
        {
            List<GameObject> spawnedInstances = new List<GameObject>();

            if (initialisationConfig == null)
            {
                initialisationConfig = Resources.Load<InitialisationConfig>(INITIALISATION_CONFIG_PATH);
            }

            if (initialisationConfig == null)
            {
              
                return spawnedInstances;
            }

           

            int maxManualLayer = initialisationConfig.manualLayers.Count > 0 ? initialisationConfig.manualLayers.Max(l => l.layerIndex) : 0;
            int maxAddressableLayer = initialisationConfig.addressableLayers.Count > 0 ? initialisationConfig.addressableLayers.Max(l => l.layerIndex) : 0;
            int totalLayers = Mathf.Max(maxManualLayer, maxAddressableLayer) + 1;

            var loadTasks = new List<UniTask<GameObject>>();

            for (int layerIndex = 0; layerIndex < totalLayers; layerIndex++)
            {
                int currentLayer = layerIndex;

                var manualLayerData = initialisationConfig.manualLayers.FirstOrDefault(l => l.layerIndex == currentLayer);
                if (manualLayerData != null)
                {
                    foreach (var prefab in manualLayerData.prefabs)
                    {
                        if (prefab == null) continue;
                        var instance = InstantiateAndRegister(prefab, currentLayer);
                        if (instance != null) spawnedInstances.Add(instance);
                    }
                }
                
                var addressableLayerData = initialisationConfig.addressableLayers.FirstOrDefault(l => l.layerIndex == currentLayer);
                if (addressableLayerData != null)
                {
                    foreach (var assetRef in addressableLayerData.addressablePrefabs)
                    {
                        if (assetRef == null) continue;
                        loadTasks.Add(LoadAndInstantiateAddressableAsync(assetRef, currentLayer));
                    }
                }
            }

            if (loadTasks.Count > 0)
            {
                var addressableResults = await UniTask.WhenAll(loadTasks);
                spawnedInstances.AddRange(addressableResults.Where(x => x != null));
            }

            return spawnedInstances;
        }

        private GameObject InstantiateAndRegister(GameObject prefab, int layerIndex)
        {
            var instance = Instantiate(prefab); 
            DontDestroyOnLoad(instance);

            if (instance.TryGetComponent(out IInitialisable initialisable)) 
            {
                RegisterInitialisableObject(initialisable, layerIndex); 
            }
            else if (instance.GetComponentInChildren<IInitialisable>() is { } childInitialisable)
            {
                RegisterInitialisableObject(childInitialisable, layerIndex);
            }
            else
            {
                Debug.LogWarning($"Prefab [{prefab.name}] (Kéo tay) không thực thi IInitialisable cả ở Root lẫn Child."); 
            }

            return instance;
        }

        private async UniTask<GameObject> LoadAndInstantiateAddressableAsync(AssetReferenceGameObject assetRef, int layerIndex)
        {
            GameObject instance = await assetRef.InstantiateAsync().Task.AsUniTask();
            addressableInstances.Add(instance);
            DontDestroyOnLoad(instance);

            if (instance.TryGetComponent(out IInitialisable initialisable))
            {
                RegisterInitialisableObject(initialisable, layerIndex);
            }
            else if (instance.GetComponentInChildren<IInitialisable>() is { } childInitialisable)
            {
                RegisterInitialisableObject(childInitialisable, layerIndex);
            }
            else
            {
                Debug.LogWarning($"Addressable Prefab [{instance.name}] không thực thi IInitialisable!");
            }

            return instance;
        }

        public async UniTask<bool> InitializeAllObjectsAsync(LoadingProgressUI loadingUI)
        {
            initializationErrors.Clear();
            _initializedObjectsCount = 0;
         
            _totalObjectsToInitialize = priorityLayers.Sum(layer => layer.Count(obj => !obj.IsInitialized));
            
            if (_totalObjectsToInitialize == 0)
            {
                if (loadingUI != null) await loadingUI.SmoothUpdateProgress(1f, cancellationToken: initializationCts.Token);
                return true;
            }

            for (int layerIndex = 0; layerIndex < priorityLayers.Count; layerIndex++)
            {
                var objectsAtLayer = priorityLayers[layerIndex].Where(obj => !obj.IsInitialized).ToList();
                if (objectsAtLayer.Count == 0) continue;
          
                var initializationTasks = objectsAtLayer.Select(async obj =>
                {
                    try 
                    {
                        var result = await obj.InitializeAsync(initializationCts.Token);
                        if (!result) throw new Exception($"Initialization returned false on {obj.GetType().Name}");

                        Interlocked.Increment(ref _initializedObjectsCount); 
                        
                        if (loadingUI != null) 
                        {
                            float targetProgress = (float)_initializedObjectsCount / _totalObjectsToInitialize; 
                            loadingUI.SmoothUpdateProgress(targetProgress, duration: 0.15f, initializationCts.Token).Forget(); 
                        }
                    }
                    catch (Exception ex) 
                    {
                        initializationErrors.Add(new InitializationError(obj, ex)); 
                    }
                });

                await UniTask.WhenAll(initializationTasks); 
                
                if (initializationErrors.Count > 0)
                {
                    Debug.LogError($"Layer {layerIndex} gặp lỗi. Dừng chuỗi khởi tạo."); 
                    foreach (var error in initializationErrors) 
                    {
                        Debug.LogError($"Thất bại tại: {error.FailedObject.GetType().Name} | Lỗi: {error.Exception.Message}"); 
                    }
                    return false; 
                }
            }

            if (loadingUI != null) await loadingUI.SmoothUpdateProgress(1f, duration: 0.1f, initializationCts.Token); 
            return initializationErrors.Count == 0; 
        }

        public async UniTask RouteAndLoadNextSceneAsync()
        {
            if (initialisationConfig.sceneLoadType == SceneLoadType.NormalSceneManager)
            {
                if (!string.IsNullOrEmpty(initialisationConfig.nextSceneName))
                {
                    var asyncOp = SceneManager.LoadSceneAsync(initialisationConfig.nextSceneName);
                    if (asyncOp != null)
                    {
                        await asyncOp.ToUniTask(cancellationToken: initializationCts.Token);
                    }
                }
                else
                {
                    Debug.LogError("[InitialiserSystem] Chưa cấu hình tên NextSceneName cho phương thức truyền thống!");
                }
            }
            else if (initialisationConfig.sceneLoadType == SceneLoadType.Addressables)
            {
                if (initialisationConfig.sceneAddressableRef != null && initialisationConfig.sceneAddressableRef.RuntimeKeyIsValid())
                {
                   
                    await Addressables.LoadSceneAsync(initialisationConfig.sceneAddressableRef).Task;
                }
                else
                {
                    Debug.LogError("[InitialiserSystem] Chưa kéo thả AssetReference Scene Addressable vào Config!");
                }
            }
        }

        protected override void OnDestroy() 
        {
            initializationCts.Cancel(); 
            initializationCts.Dispose(); 

            foreach (var instance in addressableInstances)
            {
                if (instance != null) Addressables.ReleaseInstance(instance);
            }
            addressableInstances.Clear();
            
            base.OnDestroy();
        }
    }
}