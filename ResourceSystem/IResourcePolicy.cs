using UnityEngine;

namespace ChieChie.Core
{
    public interface IResourcePolicy
    {
        bool IsInfinite(ResourceType resourceType);
    }
    public class DefaultResourcePolicy : IResourcePolicy
    {
        public bool IsInfinite(ResourceType resourceType) => false;
    }
   
}
