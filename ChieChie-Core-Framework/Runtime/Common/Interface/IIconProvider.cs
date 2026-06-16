using UnityEngine;

namespace ChieChie.Core
{
    public interface IIconProvider
    {
        Sprite GetRewardIcon(ResourceType type, bool isInfinite);
    }
}
