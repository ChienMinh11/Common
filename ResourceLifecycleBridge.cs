using ChieChie.Constracts;
using UnityEngine;
using VContainer;

namespace Game.GamePlay
{
    public class ResourceLifecycleBridge : MonoBehaviour
    {
        [Inject] private readonly IResourceService _resourceService;

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _resourceService != null)
            {
                _resourceService.OnAppPause(true); 
            }
        }

        private void OnApplicationQuit()
        {
            if (_resourceService != null)
            {
                _resourceService.OnAppQuit();
            }
        }
    }
}
