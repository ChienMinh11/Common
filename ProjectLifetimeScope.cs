using System;
using System.Collections.Generic;
using ChieChie.Core;
using ChieChie.Audio;
using ChieChie.Vibration;
using ChieChie.Resource;
using ChieChie.Shop;
using ChieChie.Localization;
using ChieChie.UI.Popups;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Example.Example.Example.Script.Scope
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private LoadingProgressUI loadingProgressUI;
        [SerializeField] private ResourceLifecycleBridge resourceLifecycleBridge;
        [SerializeField] private TransitionScreen transitionScreen;

        [Header("Debug")] 
        [SerializeField] private bool showLog = true;
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 60f;

        private Dictionary<Type, ScriptableObject> _loadedConfigs = new();

        protected override void Awake()
        {
            if (Parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            autoRun = false;
            base.Awake();
        }

        protected void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            if (loadingProgressUI != null) loadingProgressUI.ShowLoadingUI();

            IList<ScriptableObject> configs =
                await Addressables.LoadAssetsAsync<ScriptableObject>("GameConfig", null).ToUniTask();
            foreach (var config in configs)
            {
                _loadedConfigs[config.GetType()] = config;
            }

            if (loadingProgressUI != null) await loadingProgressUI.SmoothUpdateProgress(0.15f);

            Build();
            if (loadingProgressUI != null) await loadingProgressUI.SmoothUpdateProgress(0.25f);

            try
            {
                var initialiser = Container.Resolve<ServiceOrderedInitialiser>();
                float targetProgress = 0.25f;

                UniTask serviceInitTask = initialiser.StartAsync(this.destroyCancellationToken,
                    (progressValue) => { targetProgress = progressValue; });

                float duration = 2.0f;
                float elapsedTime = 0f;
                float startUIProgress = 0.25f;

                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float timeRatio = elapsedTime / duration;
                    float currentVisualProgress = Mathf.Min(
                        Mathf.Lerp(startUIProgress, 0.95f, timeRatio),
                        targetProgress
                    );

                    if (loadingProgressUI != null)
                    {
                        loadingProgressUI
                            .SmoothUpdateProgress(currentVisualProgress, duration: 0f, this.destroyCancellationToken)
                            .Forget();
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, this.destroyCancellationToken);
                }

                await serviceInitTask;

                if (loadingProgressUI != null) await loadingProgressUI.SmoothUpdateProgress(1.0f, duration: 0.3f);
                
                if (transitionScreen != null)  await transitionScreen.PlayTransitionInAsync();
                
                if (loadingProgressUI != null) loadingProgressUI.HideLoadingUI();
                
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(1)
                    .ToUniTask(cancellationToken: this.destroyCancellationToken);
                
                if (transitionScreen != null)await transitionScreen.PlayTransitionOutAsync(0.5f);
                
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProjectLifetimeScope] Lỗi chuỗi khởi tạo: {ex.Message}");
            }
        }

        private TGet GetConfig<TGet>() where TGet : ScriptableObject
        {
            if (_loadedConfigs.TryGetValue(typeof(TGet), out var config))
            {
                return config as TGet;
            }

            Debug.LogError(
                $"[ProjectLifetimeScope] Không tìm thấy Config kiểu: {typeof(TGet).Name} trong Addressables! Hãy check lại Label.");
            return null;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<TransitionEventManager>(Lifetime.Singleton);
            new EventServiceInstaller().Install(builder);
            new SaveSystemInstaller(showLog,autoSaveInterval,autoSave).Install(builder);
            new LocalizationInstaller(GetConfig<LocalizationData>()).Install(builder);
            new InternetTimeServiceInstaller(GetConfig<TimeServiceSettings>()).Install(builder);
            new IconProviderInstaller(GetConfig<IconConfigDataBase>()).Install(builder);
            new RewardDisplayServiceInstaller().Install(builder);

            builder.RegisterComponent(transitionScreen)
                .As<ITransitionScreen>()
                .As<IServiceInitialisable>();

            builder.Register<MockIapBridge>(Lifetime.Singleton)
                .As<IIapBrigde>()
                .As<IServiceInitialisable>()
                .AsSelf();
            new VibrationServiceInstaller(GetConfig<VibrationConfig>()).Install(builder);
            new AudioServiceInstaller(GetConfig<AudioConfig>()).Install(builder);
            new ResourceServiceInstaller(GetConfig<ResourceConfig>(), resourceLifecycleBridge).Install(builder);
            new PopupServiceInstaller(GetConfig<PopupConfig>()).Install(builder);
            new ShopInstaller(GetConfig<ShopConfig>()).Install(builder);

            builder.Register<ServiceOrderedInitialiser>(Lifetime.Singleton)
                .WithParameter(container => new List<List<IServiceInitialisable>>
                {
                    // --- LAYER 1
                    new()
                    {
                        container.Resolve<TransitionScreen>(),
                        container.Resolve<MockIapBridge>(),
                    },

                    // --- LAYER 2
                    new()
                    {
                        container.Resolve<AudioManager>(),
                        container.Resolve<VibrationManager>(),
                        container.Resolve<ResourceManager>(),
                        container.Resolve<PopupController>(),
                    },

                    // --- LAYER 3
                    new()
                    {
                        container.Resolve<ShopManager>(),
                    }
                })
                .WithParameter("showLog",showLog);
        }
    }
}