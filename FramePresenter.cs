using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public class FramePresenter : IFramePresenter
    {
        public event Action OnFrameListUpdated;
        public event Action<FrameModel> OnFrameUnlocked;

        public bool IsInitialized { get; private set; }
        
        private Dictionary<int, FrameModel> _frames = new Dictionary<int, FrameModel>();
        private Dictionary<int, Sprite> _frameIcons = new Dictionary<int, Sprite>();
        private Dictionary<int, GameObject> _framePrefabs = new Dictionary<int, GameObject>();
        
        private readonly IProfileSaveAdapter _saveAdapter;
        private readonly FrameConfig _frameConfig;
        
        public FramePresenter(IProfileSaveAdapter saveAdapter, FrameConfig frameConfig)
        {
            _saveAdapter = saveAdapter;
            _frameConfig = frameConfig;
        }
        
        public bool Initialize()
        {
            if (_frameConfig == null)
            {
                Debug.LogError("[FramePresenter] Frame config is not assigned!");
                return false;
            }
            
            LoadFrames();
            IsInitialized = true;
            return true;
        }
        
        private void LoadFrames()
        {
            var rootData = _saveAdapter.LoadData();
            var savedFrames = rootData.frames;
    
            if (savedFrames == null || savedFrames.Count == 0)
            {
                _frames = _frameConfig.GetDefaultFrameInfoDictionary();
                rootData.frames = _frames;
                _saveAdapter.SaveData(rootData);
            }
            else
            {
                _frames = savedFrames;
                foreach (var frameData in _frameConfig.Frames)
                {
                    if (!_frames.ContainsKey(frameData.Id))
                    {
                        var newInfo = frameData.ToFrameInfo();
                        newInfo.IsUnlocked = !frameData.LockByDefault; 
                       _frames[frameData.Id] = newInfo;
                    }
                }
                _saveAdapter.SaveData(rootData);
            }
            
            foreach (var frameData in _frameConfig.Frames)
            {
                if (frameData.FrameIcon != null) _frameIcons[frameData.Id] = frameData.FrameIcon;
                if (frameData.FramePrefab != null) _framePrefabs[frameData.Id] = frameData.FramePrefab;
            }
            
            OnFrameListUpdated?.Invoke();
        }
        
        public List<FrameModel> GetAllFrames() => new List<FrameModel>(_frames.Values);
        public FrameModel GetFrame(int frameId) => _frames.TryGetValue(frameId, out var frame) ? frame : null;
        public GameObject GetFramePrefab(int frameId) => _framePrefabs.TryGetValue(frameId, out var prefab) ? prefab : null;
        public Sprite GetFrameSprite(int frameId) => _frameIcons.TryGetValue(frameId, out var sprite) ? sprite : null;

        public bool LockFrame(int frameId)
        {
            if (_frames.TryGetValue(frameId, out var frame))
            {
                if (!frame.IsUnlocked) return false; 
        
                frame.IsUnlocked = false;
                
                var rootData = _saveAdapter.LoadData();
                rootData.frames = _frames;

                if (rootData.profile != null && rootData.profile.FrameId == frameId)
                {
                    int fallbackId = 0;
                    foreach (var pair in _frames)
                    {
                        if (pair.Value.IsUnlocked)
                        {
                            fallbackId = pair.Key;
                            break;
                        }
                    }
                    rootData.profile.FrameId = fallbackId;
                }

                _saveAdapter.SaveData(rootData);
                OnFrameListUpdated?.Invoke(); 
                return true;
            }
            return false;
        }

        public bool UnlockFrame(int frameId)
        {
            if (_frames.TryGetValue(frameId, out var frame))
            {
                if (frame.IsUnlocked) return false; 
                
                frame.IsUnlocked = true;
                
                var rootData = _saveAdapter.LoadData();
                rootData.frames = _frames;
                _saveAdapter.SaveData(rootData);

                OnFrameUnlocked?.Invoke(frame);
                return true;
            }
            return false;
        }
        
        public void UnlockAllFrames()
        {
            bool anyUnlocked = false;
            foreach (var frame in _frames.Values)
            {
                if (!frame.IsUnlocked)
                {
                    frame.IsUnlocked = true;
                    anyUnlocked = true;
                    OnFrameUnlocked?.Invoke(frame);
                }
            }
    
            if (anyUnlocked)
            {
                var rootData = _saveAdapter.LoadData();
                rootData.frames = _frames;
                _saveAdapter.SaveData(rootData);

                OnFrameListUpdated?.Invoke();
            }
        }

        public bool IsFrameUnlocked(int frameId) => _frames.TryGetValue(frameId, out var frame) && frame.IsUnlocked;
    }
}