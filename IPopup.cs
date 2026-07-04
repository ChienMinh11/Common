using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ChieChie.Core
{
  
    public interface IPopup
    {
        string PopupNameId { get; }
        Transform transform { get; } 
        bool IsCacheable { get; }
        void Initialize();
        bool CanAutoShow();
        UniTask Show();
        UniTask Hide(); 
        Action<IPopup> OnClosed { get; set; }
        Action<IPopup> OnRequestClose { get; set; }
        Action<IPopup> OnRequestCloseAll { get; set; }
        Action<IPopup> OnHideRootCanvas { get; set; }
    }

    public interface IPopupWithData : IPopup
    {
        void SetMessage(string message);
    }
}
