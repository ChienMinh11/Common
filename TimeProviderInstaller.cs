using System;
using ChieChie.Constracts;
using Game.GamePlay;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.DependencyInjection
{
    public class TimeProviderInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
#if DEVELOPMENT_BUILD  || UNITY_EDITOR
            builder.Register<VirtualTimeProvider>(Lifetime.Singleton).As<ITimeProvider>().AsSelf();
           
#else
           builder.Register<RealTimeProvider>(Lifetime.Singleton).As<ITimeProvider>().AsSelf();
#endif
        }
    }
}
