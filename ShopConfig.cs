using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "CORE/Feature/ShopConfig")]
    public class ShopConfig : ScriptableObject
    {
        public List<ShopItemData> shopItems = new List<ShopItemData>();
        
    }
 
}
