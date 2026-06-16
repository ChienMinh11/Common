using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class InternetTimeServiceInstaller : IInstaller
    {
        private readonly TimeServiceSettings _timeSettings;
        
        public InternetTimeServiceInstaller(TimeServiceSettings timeSettings)
        {
            _timeSettings = timeSettings;
        }
        
        public void Install(IContainerBuilder builder)
        {
            if (_timeSettings != null)
            {
                builder.RegisterInstance(_timeSettings);
                builder.Register<InternetTimeService>(Lifetime.Singleton)
                    .As<IInternetTimeService>();


            }
        }
    }
}
