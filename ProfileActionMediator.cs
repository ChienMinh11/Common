using System;
using ChieChie.Core;
using ChieChie.Profile;
using UnityEngine;
using VContainer.Unity;
using Cysharp.Threading.Tasks; // Đảm bảo có UniTask nếu dùng async

namespace Game.GamePlay
{
    public class ProfileActionMediator : IStartable, IDisposable
    {
        private readonly IPopupService _popupService;
        private readonly IProfileService _profileService;
        
        public ProfileActionMediator(IPopupService popupService, IProfileService profileService)
        {
            _popupService = popupService;
            _profileService = profileService;
        }
        
        public void Start()
        {
            _profileService.OnEditNameRequested += HandleEditNameRequested;
            _profileService.OnCloseRequested += HandleCloseRequested;
        }

        public void Dispose()
        {
            _profileService.OnEditNameRequested -= HandleEditNameRequested;
            _profileService.OnCloseRequested -= HandleCloseRequested;
        }

        private void HandleEditNameRequested()
        {
            OpenInputPopupAsync().Forget();
        }

        private async UniTaskVoid OpenInputPopupAsync()
        {
            if (_popupService == null) return;

            await _popupService.ShowPopup("PopupProfileInputText");
            var inputTextPopup = _popupService.GetPopup<PopupProfileInputText>("PopupProfileInputText");
            if (inputTextPopup != null)
            {
                inputTextPopup.SetTittleText("Edit Player Name");
                inputTextPopup.SetContinueCallback(OnNameInputComplete);
            }
        }

        private void OnNameInputComplete(string newName)
        {
            _profileService.ChangeTemporaryName(newName);
        }
 
        private void HandleCloseRequested()
        {
            if (_profileService.HasChanges())
            {
               
                _popupService.ShowPopup("PopupProfileMessage");
            }
            else
            {
                
                var profilePopup = _popupService.GetPopup<PopupProfile>("PopupProfile");
                if (profilePopup != null)
                {
                    profilePopup.ForceClose();         
                }
            }
        }
    }
}