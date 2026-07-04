using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
   
    [CreateAssetMenu(fileName = "PopupConfig", menuName = "ChieChie/UI/Popup Config")]
    public class PopupConfig : ScriptableObject
    {
        [Header("Registry & Config")]
        [SerializeField] private List<MonoBehaviour> _popupRegistryComponents = new List<MonoBehaviour>();
        [SerializeField] private string _resourcesPath = "Popups/";
        public List<MonoBehaviour> PopupRegistryComponents => _popupRegistryComponents;
        public string ResourcesPath => _resourcesPath;
    }
}
