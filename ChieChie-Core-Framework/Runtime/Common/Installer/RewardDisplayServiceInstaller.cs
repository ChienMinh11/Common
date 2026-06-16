using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class RewardDisplayServiceInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<RewardDisplayService>(Lifetime.Singleton);
        }
    }
}
