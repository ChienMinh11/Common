using System;
using UnityEngine;

namespace ChieChie.Core
{
    [Serializable] 
    public abstract class BaseLevelData
    {
        
        [SerializeField] 
        private int _version = 1;
    
        public int version 
        { 
            get => _version;
            set => _version = value;
        }
    
        [NonSerialized]
        private int _latestVersion;
        
        protected abstract int LATEST_VERSION { get; }
    
        public void UpdateToLatestVersion()
        {
            if (version == LATEST_VERSION) return;
        
            Debug.Log($"Upgrading from version {version} to {LATEST_VERSION}");
        
            try 
            {
                int maxIterations = 100;
                int iterations = 0;
            
                while (version < LATEST_VERSION && iterations < maxIterations)
                {
                    var oldVersion = version;
                    version++;
                    ApplyUpdate(version);
                    Debug.Log($"Successfully upgraded from v{oldVersion} to v{version}");
                    iterations++;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during version upgrade: {e.Message}");
            }
        }

        protected abstract void ApplyUpdate(int targetVersion);

        [System.Runtime.Serialization.OnDeserialized]
        private void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
            UpdateToLatestVersion();
        }
    }
}