using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IPopupService
    {
        CanvasGroup CanvasGroup { get; }
        bool IsBackground { get; set; }
        UniTask<bool> ShowPopup(string popupNameId, string message = "", bool closeAndRestore = false);
        T GetPopup<T>(string popupNameId) where T : class, IPopup;
        void CloseLastPopup();
        void CloseAllPopups(bool noFade = false);
        bool HasActivePopups();
        void SetActive(bool active);
        void BindSceneCanvas(Canvas newCanvas, Transform up, Transform down, CanvasGroup group);
        void UnbindSceneCanvas();
    }
    public interface IPopup
    {
        string PopupNameId { get; }
        Transform transform { get; } 
        
        void Initialize(IEventService eventService);
        bool IsCacheable { get; }
        bool CanShow();
        UniTask Show();
        UniTask Hide(); 
    }

    public interface IPopupWithData : IPopup
    {
        void SetMessage(string message);
    }
}
