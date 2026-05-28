using System;
using UnityEngine;

namespace ChieChie.Core
{
    public interface ISaveSystem
    {
        bool IsInitialized { get; }
    
        void RegisterKey<T>(string key, Func<T> getRuntimeValueMethod, bool isAutoSave = true);
        void RegisterKey(string key);
        
        bool IsKeyRegistered(string key);
        
        void Save<T>(string key, T value);
        T Load<T>(string key, T defaultValue = default);
        
        void Delete(string key);
        void DeleteAll();

        void TriggerAutoSave();
    }
}
