using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class IconProviderInstaller : IInstaller
    {
        private readonly IconConfigDataBase _iconConfigData;
        
        public IconProviderInstaller(IconConfigDataBase iconConfigData)
        {
            _iconConfigData = iconConfigData; }

        public void Install(IContainerBuilder builder)
        {
            if (_iconConfigData != null)
            {
                builder.RegisterInstance(_iconConfigData).As<IIconProvider>().AsSelf();
            }
        }
    }
}
