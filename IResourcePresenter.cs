using ChieChie.Core;

namespace ChieChie.Resource
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
