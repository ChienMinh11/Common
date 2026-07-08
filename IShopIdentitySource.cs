using UnityEngine;

namespace ChieChie.Shop
{
    public interface IShopIdentitySource 
    {
        string ResourceId { get; }
        Sprite Icon { get; }
        Sprite InfinityIcon { get; }
    }
}
