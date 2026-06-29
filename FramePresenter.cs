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
        private Dictionary<int, Sprite> _frameSprites = new Dictionary<int, Sprite>();
        
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
            
            _saveAdapter.RegisterFramesKey(() => _frames);
            LoadFrames();
            
            IsInitialized = true;
            return true;
        }
        
        private void LoadFrames()
        {
            var savedFrames = _saveAdapter.LoadFrames();
            
            if (savedFrames == null || savedFrames.Count == 0)
            {
                _frames = _frameConfig.GetDefaultFrameInfoDictionary();
                _saveAdapter.SaveFrames(_frames);
            }
            else
            {
                _frames = savedFrames;
                foreach (var frameData in _frameConfig.Frames)
                {
                    if (!_frames.ContainsKey(frameData.Id))
                    {
                        _frames[frameData.Id] = frameData.ToFrameInfo();
                    }
                }
                _saveAdapter.SaveFrames(_frames);
            }
            
            foreach (var frameData in _frameConfig.Frames)
            {
                if (frameData.FrameSprite != null)
                {
                    _frameSprites[frameData.Id] = frameData.FrameSprite;
                }
            }
            
            OnFrameListUpdated?.Invoke();
        }
        
        public List<FrameModel> GetAllFrames()
        {
            return new List<FrameModel>(_frames.Values);
        }
  
        public FrameModel GetFrame(int frameId)
        {
            return _frames.TryGetValue(frameId, out var frame) ? frame : null;
        }
     
        public Sprite GetFrameSprite(int frameId)
        {
            return _frameSprites.TryGetValue(frameId, out var sprite) ? sprite : null;
        }

        public bool UnlockFrame(int frameId)
        {
            if (_frames.TryGetValue(frameId, out var frame))
            {
                if (frame.IsUnlocked) return false; 
                
                frame.IsUnlocked = true;
                _saveAdapter.SaveFrames(_frames);
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
                _saveAdapter.SaveFrames(_frames);
                OnFrameListUpdated?.Invoke();
            }
        }

        public bool IsFrameUnlocked(int frameId)
        {
            return _frames.TryGetValue(frameId, out var frame) && frame.IsUnlocked;
        }
    }
}