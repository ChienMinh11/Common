using UnityEngine;

namespace ChieChie.Constracts
{
    public interface IResourceIdentitySource
    {
        string ResourceId { get; }
        string DisplayName { get; }
        Sprite Icon { get; }
        Sprite InfinityIcon { get; }
    }
}
