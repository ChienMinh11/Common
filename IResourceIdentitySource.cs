using UnityEngine;

namespace ChieChie.Resource
{
    public interface IResourceIdentitySource
    {
        string ResourceId { get; }
        string DisplayName { get; }
        Sprite Icon { get; }
        Sprite InfinityIcon { get; }
    }
}
