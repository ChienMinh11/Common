using System;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IResourceManager
    {
        bool IsInitialized { get; }
        
        // API Đăng ký dành cho UI Controller
        IResourcePresenter RegisterView(ResourceType resourceType, IResourceView view);
        void UnregisterPresenter(IResourcePresenter presenter);

        // API Gameplay thông thường
        void AddResource(ResourceType resourceType, long amount, bool delayUpdate = false);
        bool SpendResource(ResourceType resourceType, long amount);
        long GetCurrentAmount(ResourceType resourceType);
        bool IsAtMaxStack(ResourceType resourceType);
        void ProcessPendingUpdate(ResourceType resourceType);
        void ForceUpdateAllView();

        // --- API MỞ RỘNG CHO INFINITY SYSTEM VẪN ĐẢM BẢO TÍNH ĐỘC LẬP ---
        void AddInfiniteDuration(ResourceType resourceType, TimeSpan duration);
        bool IsCurrentlyInfinite(ResourceType resourceType);
        TimeSpan GetRemainingInfiniteTime(ResourceType resourceType);
    }
}
