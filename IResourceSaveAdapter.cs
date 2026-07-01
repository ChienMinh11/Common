using System;

namespace ChieChie.Constracts
{
    public interface IResourceSaveAdapter
    {
        void RegisterResource(IResourceData resourceData);

        void SaveAmount(IResourceData resourceData, long amount);
        long LoadAmount(IResourceData resourceData, long fallbackValue);

        void SaveInfiniteExpiration(IResourceData resourceData, DateTime expirationTime);
        DateTime LoadInfiniteExpiration(IResourceData resourceData, DateTime fallbackValue);

        void SaveRegenStatus(IResourceData resourceData, bool isEnabled);
        bool LoadRegenStatus(IResourceData resourceData, bool defaultValue);
        
        void SaveNextRegenTime(IResourceData resourceData, DateTime nextRegenTime);
        DateTime LoadNextRegenTime(IResourceData resourceData, DateTime fallbackValue);

        bool IsFirstInit();
        void SetFirstInitComplete();
    }
}