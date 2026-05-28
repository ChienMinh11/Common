using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private InitialiserSystem initialiserSystem;
        [SerializeField] private LoadingProgressUI loadingProgressUI;

        private List<GameObject> _runtimeInstances = new();

        protected override void Awake()
        {
            autoRun = false;
            base.Awake();
        }

        protected async void Start()
        {
            if (initialiserSystem == null)
            {
                initialiserSystem = InitialiserSystem.Instance;
            }

            if (loadingProgressUI != null) loadingProgressUI.ShowLoadingUI();

            _runtimeInstances = await initialiserSystem.PrepareAndLoadAllLayersAsync();
         
            Build();
           
            foreach (var instance in _runtimeInstances)
            {
                if (instance != null)
                {
                    Container.InjectGameObject(instance);
                }
            }
         
            bool success = await initialiserSystem.InitializeAllObjectsAsync(loadingProgressUI);

            if (success)
            {
                Debug.Log("<color=green>[VContainer - Initialiser]</color> Khởi tạo thành công! Đang chuyển scene...");

                TransitionScreen transitionScreen = null;
                try
                {
                    transitionScreen = Container.Resolve<TransitionScreen>();
                }
                catch (System.Exception)
                {
                    foreach (var instance in _runtimeInstances)
                    {
                        if (instance != null && instance.TryGetComponent(out transitionScreen))
                        {
                            break;
                        }
                    }
                }

                if (transitionScreen != null)
                {
                    await transitionScreen.PlayTransitionInAsync();
                }

                if (loadingProgressUI != null) loadingProgressUI.HideLoadingUI();

                await initialiserSystem.RouteAndLoadNextSceneAsync();

                if (transitionScreen != null)
                {
                    await transitionScreen.PlayTransitionOutAsync();
                }
            }
            else
            {
                Debug.LogError("[VContainer - Initialiser] Quá trình khởi tạo thất bại!");
            }

            if (loadingProgressUI != null) loadingProgressUI.HideLoadingUI();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<EventService>(Lifetime.Singleton).As<IEventService>();

            if (_runtimeInstances != null && _runtimeInstances.Count > 0)
            {
                RegisterDynamicServices(builder, _runtimeInstances);
            }

            builder.Register<PopupQueueManager>(Lifetime.Singleton).As<IPopupQueueService>();
        }

        private void RegisterDynamicServices(IContainerBuilder builder, List<GameObject> instances)
        {
            foreach (var instance in instances)
            {
                if (instance == null) continue;

                IInitialisable targetService = null;
                Component componentInstance = null;

                if (instance.TryGetComponent(out IInitialisable rootService))
                {
                    targetService = rootService;
                    componentInstance = rootService as Component;
                }
                else if (instance.GetComponentInChildren<IInitialisable>() is { } childService)
                {
                    targetService = childService;
                    componentInstance = childService as Component;
                }

                if (targetService != null && componentInstance != null)
                {
                    var registration = builder.RegisterComponent(componentInstance);

                    foreach (var @interface in targetService.GetType().GetInterfaces())
                    {
                        if (@interface != typeof(IInitialisable))
                        {
                            registration.As(@interface);
                        }
                    }

                    registration.AsSelf();
                }
            }
        }
    }
}