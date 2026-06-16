using System;
using ChieChie.Core;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Resource
{
    public class ResourceServiceInstaller : IInstaller
    {
        private readonly ResourceConfig _config;
        private readonly ResourceLifecycleBridge _resourceLifecycleBridge;

        public ResourceServiceInstaller(ResourceConfig config,ResourceLifecycleBridge resourceLifecycleBridge)
        {
            _config = config;
            _resourceLifecycleBridge = resourceLifecycleBridge;
        }
        public void Install(IContainerBuilder builder)
        {
            if (_config != null) builder.RegisterInstance(_config);
            builder.Register<ResourceManager>(Lifetime.Singleton)
                .As<IResourceService>()
                .As<IDisposable>()
                .As<IServiceInitialisable>()
                .AsSelf();

            builder.RegisterComponent(_resourceLifecycleBridge);
        }
    }
}
