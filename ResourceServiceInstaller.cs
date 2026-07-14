using System;
using ChieChie.Constracts;
using ChieChie.Resource;
using Game.GamePlay;
using VContainer;
using VContainer.Unity;

namespace Game.DependencyInjection
{
    public class ResourceServiceInstaller : IInstaller
    {
        private readonly ResourceConfig _config;
        private readonly ResourceLifecycleBridge _resourceLifecycleBridge;

        public ResourceServiceInstaller(ResourceConfig config, ResourceLifecycleBridge _bridge)
        {
            _config = config;
            _resourceLifecycleBridge = _bridge;
        }

        public void Install(IContainerBuilder builder)
        {
            if (_config != null) builder.RegisterInstance(_config);
   
            builder.Register<ResourceSaveAdapter>(Lifetime.Singleton).As<IResourceSaveAdapter>();

            builder.Register<ResourceManager>(Lifetime.Singleton)
                .As<IResourceService>()
                .As<IDisposable>()
                .AsSelf();

            builder.RegisterComponent(_resourceLifecycleBridge);
        }
    }
}
