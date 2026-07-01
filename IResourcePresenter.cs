
namespace ChieChie.Constracts
{
    public interface IResourcePresenter
    {
        string ResourceKey { get; }
        bool HasPendingUpdates { get; }
        bool IsInfiniteUpdateDelayed { get; }
        void ProcessPendingUpdates();
        void ForceUpdateView();
        void Cleanup();
    }
}
