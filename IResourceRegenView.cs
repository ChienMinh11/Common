using ChieChie.Core;

namespace ChieChie.Resource
{
    public interface IResourceRegenView : IResourceView
    {
        void SetRegenStatusActive(bool isActive);
        void SetRegenStatusText(string text);
    }
}
