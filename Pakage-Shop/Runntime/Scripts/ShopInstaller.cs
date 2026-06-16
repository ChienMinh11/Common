using ChieChie.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Shop
{
    public class ShopInstaller : IInstaller
    {
        private readonly ShopConfig _config;

        public ShopInstaller(ShopConfig config)
        {
            _config = config;
        }
        public void Install(IContainerBuilder builder)
        {
            if(_config!=null) builder.RegisterInstance(_config);
            
            builder.Register<ShopManager>(Lifetime.Singleton)
                .As<IShopService>()
                .As<IServiceInitialisable>()
                .AsSelf();
        }
    }
}
