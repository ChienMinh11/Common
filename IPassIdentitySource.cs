using UnityEngine;

namespace ChieChie.GamePass
{
    public interface IPassIdentitySource
    {
        string ResourceId { get; }
        Sprite Icon { get; }
        Sprite InfinityIcon { get; }
    }
}
