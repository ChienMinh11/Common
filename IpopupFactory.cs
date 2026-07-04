using UnityEngine;

namespace ChieChie.Core
{
    public interface IpopupFactory 
    {
        GameObject CreatePopup(GameObject prefab, Transform parent);
    }
}
