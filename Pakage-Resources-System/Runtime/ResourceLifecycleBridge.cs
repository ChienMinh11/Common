using UnityEngine;
using VContainer;

namespace ChieChie.Resource
{
    public class ResourceLifecycleBridge : MonoBehaviour
    {
        [Inject] private readonly ResourceManager _resourceManager;

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _resourceManager != null)
            {
                _resourceManager.OnAppPause(true); 
            }
        }

        private void OnApplicationQuit()
        {
            if (_resourceManager != null)
            {
                _resourceManager.OnAppQuit();
            }
        }
    }
}
