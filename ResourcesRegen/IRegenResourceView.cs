using UnityEngine;

namespace ChieChie.Core
{
    public interface IRegenResourceView : IResourceView
    {
        void UpdateSubStatusText(string text, bool isVisible); 
    }
}
