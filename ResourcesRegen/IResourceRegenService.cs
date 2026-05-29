using UnityEngine;

namespace ChieChie.Core
{
    public interface IResourceRegenService
    {
        void SetRegenStatus(ResourceType type, bool isEnabled);
        bool IsRegenEnabled(ResourceType type);
        void SetRegenAmount(ResourceType type, long newAmount); 
        float GetCurrentTimer(ResourceType type);
        ResourceRegenConfig GetRegenConfig();
    }
}
