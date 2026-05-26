using UnityEngine;

namespace MyFramework
{
    public class ResourceViewInitializer : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceId;
        
        public ResourceType ResourceId => resourceId;
        private ResourceManager resourceManager;
        private IResourceView view;

        private void Awake()
        {
            resourceManager = ServiceLocator.GetService<ResourceManager>();
            view = GetComponent<IResourceView>();
        }

        private void Start()
        {
            // Chỉ đăng ký với ResourceManager
            if (resourceManager != null && view != null)
            {
                resourceManager.RegisterView(resourceId, view);
            }
        }

        private void OnDestroy()
        {
            // Hủy đăng ký
            if (resourceManager != null && view != null)
            {
                resourceManager.UnregisterView(resourceId, view);
            }
        }
    }
}