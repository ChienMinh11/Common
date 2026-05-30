using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public class SaveSystem : MonoBehaviour, ISaveSystem, IInitialisable
    {
        [SerializeField] private bool _showLog = true;
        [SerializeField] private float _autoSaveInterval = 60f;
        [SerializeField] private bool _isAutoSaveEnabled = true;

        public int InitializationPriority => 0;
        public bool IsInitialized { get; private set; }

        private ISaveLoadStrategy _saveLoadStrategy;
        private readonly Dictionary<string, KeyInfo> _registeredKeys = new Dictionary<string, KeyInfo>();
        private CancellationTokenSource _autoSaveCancellationTokenSource;

        private class KeyInfo
        {
            public bool IsAutoSave { get; set; }
            public Func<object> GetRuntimeValue { get; set; }

            public KeyInfo(bool isAutoSave, Func<object> getRuntimeValue)
            {
                IsAutoSave = isAutoSave;
                GetRuntimeValue = getRuntimeValue;
            }
        }

        public void Setup(ISaveLoadStrategy saveLoadStrategy)
        {
            _saveLoadStrategy = saveLoadStrategy;
        }

        public  UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
        
            if (_saveLoadStrategy == null)
            {
                _saveLoadStrategy = new EasySaveLoadStrategy(); 
            }
          
            _autoSaveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, 
                this.GetCancellationTokenOnDestroy()
            );

            if (_isAutoSaveEnabled)
            {
                StartAutoSave(_autoSaveCancellationTokenSource.Token).Forget();
            }

            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        public void RegisterKey<T>(string key, Func<T> getRuntimeValueMethod, bool isAutoSave = true)
        {
            if (!_registeredKeys.ContainsKey(key))
            {
                _registeredKeys[key] = new KeyInfo(isAutoSave, () => getRuntimeValueMethod());
            }
        }

        public void RegisterKey(string key)
        {
            if (!_registeredKeys.ContainsKey(key))
            {
                _registeredKeys[key] = new KeyInfo(false, null);
            }
        }

        public bool IsKeyRegistered(string key) => _registeredKeys.ContainsKey(key);

        public void Save<T>(string key, T value)
        {
            if (!IsKeyRegistered(key)) return;
            try
            {
                _saveLoadStrategy.Save(key, value);
                if (_showLog) Debug.Log($"[SaveSystem] Saved key: {key}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Error saving key '{key}': {e.Message}");
            }
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            if (!IsKeyRegistered(key)) return defaultValue;
            try
            {
                return _saveLoadStrategy.Load(key, defaultValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Error loading key '{key}': {e.Message}");
                return defaultValue;
            }
        }

        private async UniTaskVoid StartAutoSave(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_autoSaveInterval), cancellationToken: cancellationToken);
                TriggerAutoSave();
            }
        }

        public void TriggerAutoSave()
        {
            foreach (var kvp in _registeredKeys)
            {
                string key = kvp.Key;
                var keyInfo = kvp.Value;

                if (keyInfo.IsAutoSave && keyInfo.GetRuntimeValue != null)
                {
                    try
                    {
                        object latestValue = keyInfo.GetRuntimeValue.Invoke();
                        _saveLoadStrategy.Save(key, latestValue);
                        if (_showLog) Debug.Log($"[SaveSystem][AutoSave] Key '{key}' auto-saved.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SaveSystem][AutoSave] Error auto-saving key '{key}': {e.Message}");
                    }
                }
            }
        }

        public void Delete(string key)
        {
            if (!IsKeyRegistered(key)) return;
            _saveLoadStrategy.Delete(key);
        }

        public void DeleteAll() => _saveLoadStrategy.DeleteAll();

        private void OnDestroy()
        {
            _autoSaveCancellationTokenSource?.Cancel();
            _autoSaveCancellationTokenSource?.Dispose();
        }
    }
}