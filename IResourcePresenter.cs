using ChieChie.Core;

namespace ChieChie.Resource
{
    public interface IResourcePresenter
    {
        int ResourceHash { get; }
        bool HasPendingUpdates { get; }
        void ProcessPendingUpdates();
        void ForceUpdateView();
        void Cleanup();
    }
}
