using ChieChie.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.GamePlay
{
    public class GameScope : LifetimeScope
    {
        
        [SerializeField] ParticleConfigSO particleConfig;
        [SerializeField] TabGroup tabGroup;
      

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<TransformRegistry>(Lifetime.Scoped);
            new PoolInstaller().Install(builder);
            new ParticleInstaller(particleConfig).Install(builder);
            new EffectSequenceInstaller().Install(builder);
            builder.Register<IParticlePoolService, ParticlePoolAdapter>(Lifetime.Singleton);
            builder.Register<IAudioPool, AudioPoolAdapter>(Lifetime.Singleton);
            
            builder.RegisterComponent(tabGroup);
            BuildEntryPoint(builder);
            builder.RegisterBuildCallback(InitAll);
        }

        private static void BuildEntryPoint(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ShopActionMediator>(Lifetime.Scoped);
            builder.RegisterEntryPoint<ProfileActionMediator>(Lifetime.Scoped);
        }

        private void InitAll(IObjectResolver container)
        {
            var particlePoolService = container.Resolve<IParticlePoolService>();
            particlePoolService.CreatePools(); 
            var audioService = container.Resolve<IAudioService>();
            var audioPool = container.Resolve<IAudioPool>();
            if (audioService != null && audioPool !=null) audioService.CreateAudioSourcePool(audioPool);
           
        }
     
    }
}
