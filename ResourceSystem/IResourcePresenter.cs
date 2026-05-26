using UnityEngine;

namespace GameCore.Runtime
{
    public interface IResourcePresenter
    {
        ResourceType ResourceId { get; }
        bool HasPendingUpdates { get; }
        void ProcessPendingUpdates();
        void ForceUpdateView();
        void Cleanup();
    }
}
