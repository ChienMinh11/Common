using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    public interface IFramePresenter
    {
        event Action OnFrameListUpdated;
        event Action<FrameModel> OnFrameUnlocked;

        bool Initialize();
        List<FrameModel> GetAllFrames();
        FrameModel GetFrame(int frameId);
        Sprite GetFrameSprite(int frameId);
        bool UnlockFrame(int frameId);
        void UnlockAllFrames();
        bool IsFrameUnlocked(int frameId);
        GameObject GetFramePrefab(int frameId);
    }
}