using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
    public interface IPopupService
    {
        CanvasGroup CanvasGroup { get; }
        bool IsBackground { get; set; }
        UniTask<IPopup> PreloadPopup(string popupNameId);
        UniTask<bool> ShowPopup(string popupNameId, string message = "", bool closeAndRestore = false);
        T GetPopup<T>(string popupNameId) where T : class, IPopup;
        void CloseLastPopup();
        void CloseAllPopups(bool noFade = false);
        bool HasActivePopups();
        void SetActive(bool active);
        void BindSceneCanvas(Canvas newCanvas, Transform up, Transform down, CanvasGroup group);
        void UnbindSceneCanvas();
        void ReleaseUnusedPopup(string popupNameId);
        
        Action<IPopup> OnPopupClosedAction { get; set; }
        Action<IPopup> OnPopupChangedAction { get; set; }
    }
}
