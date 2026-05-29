using TMPro;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IResourceRegenView : IResourceView
    {
      
        TextMeshProUGUI StatusText { get; }
       
        GameObject StatusContainer { get; }
    }
}
