using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class EventServiceInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<EventService>(Lifetime.Singleton).As<IEventService>();
        }
    }
}
