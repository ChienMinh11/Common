using ChieChie.Constracts;
using ChieChie.Profile;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.DependencyInjection
{
    public class ProfileInstaller : IInstaller
    {
        private readonly  ProfileDatabase _database;

        public ProfileInstaller(ProfileDatabase database)
        {
            _database = database;
        }
        public void Install(IContainerBuilder builder)
        {
            if(_database!=null) builder.RegisterInstance(_database);
            builder.Register<ProfileSaveAdapter>(Lifetime.Singleton)
                .As<IProfileSaveAdapter>();
            builder.Register<ProfileManager>(Lifetime.Singleton)
                .As<IProfileService>()
                .AsSelf();
        }
    }
}
