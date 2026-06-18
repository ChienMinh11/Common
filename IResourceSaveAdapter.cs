using System;

namespace ChieChie.Resource
{
    public interface IResourceSaveAdapter
    {
        void RegisterResource(ResourceData resourceData);

        void SaveAmount(ResourceData resourceData, long amount);
        long LoadAmount(ResourceData resourceData, long fallbackValue);

        void SaveInfiniteExpiration(ResourceData resourceData, DateTime expirationTime);
        DateTime LoadInfiniteExpiration(ResourceData resourceData, DateTime fallbackValue);

        void SaveRegenStatus(ResourceData resourceData, bool isEnabled);
        bool LoadRegenStatus(ResourceData resourceData, bool defaultValue);
        
        void SaveNextRegenTime(ResourceData resourceData, DateTime nextRegenTime);
        DateTime LoadNextRegenTime(ResourceData resourceData, DateTime fallbackValue);

        bool IsFirstInit();
        void SetFirstInitComplete();
    }
}