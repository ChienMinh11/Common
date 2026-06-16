using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class SaveSystemInstaller : IInstaller
    {
        private readonly bool _showLog;
        private readonly float _autoSaveInterval;
        private readonly bool _isAutoSaveEnabled;

        public SaveSystemInstaller(bool showLog, float autoSaveInterval, bool isAutoSaveEnabled)
        {
            _showLog = showLog;
            _autoSaveInterval = autoSaveInterval;
            _isAutoSaveEnabled = isAutoSaveEnabled;
        }
        public void Install(IContainerBuilder builder)
        {
            
            builder.Register<SaveSystem>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .WithParameter("showLog", _showLog)
                .WithParameter("autoSaveInterval", _autoSaveInterval)
                .WithParameter("isAutoSaveEnabled", _isAutoSaveEnabled);
        }
    }
}
