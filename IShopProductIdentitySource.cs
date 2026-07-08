using UnityEngine;

namespace ChieChie.Shop
{
    public interface IShopProductIdentitySource 
    {
        string ProductId { get; }
        string PopupName { get; }
    }
}
