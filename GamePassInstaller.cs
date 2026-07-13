using ChieChie.GamePass;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.DependencyInjection
{
    public class GamePassInstaller : IInstaller
    {
        private readonly PassDatabase _passDatabase;

        public GamePassInstaller(PassDatabase passDatabase)
        {
            _passDatabase = passDatabase;
        }
        public void Install(IContainerBuilder builder)
        {
           if(_passDatabase!=null)builder.RegisterInstance<PassDatabase>(_passDatabase);
           builder.Register<GamePassSaveAdapter>(Lifetime.Singleton)
               .As<IPassSaveAdapter>();
           builder.Register<PassModel>(Lifetime.Singleton)
               .As<IPassService>()
               .AsSelf();
        }
    }
}
