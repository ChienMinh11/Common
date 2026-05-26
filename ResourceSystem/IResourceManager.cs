using UnityEngine;

namespace GameCore.Runtime
{
    public interface IResourceManager
    {
        bool IsInitialized { get; }
        
        // API Đăng ký dành cho UI Controller
        IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view);
        void UnregisterPresenter(IResourcePresenter presenter);

        // API Gameplay
        void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false);
        bool SpendResource(ResourceType resourceType, long amount);
        long GetCurrentAmount(ResourceType resourceType);
        bool IsAtMaxStack(ResourceType resourceType);
    }
}
