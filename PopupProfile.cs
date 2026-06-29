using ChieChie.Core;
using ChieChie.Profile;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class PopupProfile : PopupBase
    {
        [SerializeField] private ProfileView profileView;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;
        
        private IProfileService _profileManager;
        private IPopupService _popup;

        [Inject]
        private void Construct(IProfileService profileService, IPopupService popup)
        {
            _popup = popup;
            _profileManager = profileService;
        }
      
        protected override void SetPopupName() => PopupName = "PopupProfile";
        protected override void SetCacheable() => IsCache = false;

        protected override void OnShow()
        {
            SetupUI();
            
            if (_profileManager != null && profileView != null)
            {
                _profileManager.OnStateChanged += UpdateSaveButtonState; 
                _profileManager.RegisterView(profileView);
            }
        }

        private void SetupUI()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnClickClose);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(SaveChangesAndClose);
            }
        }

        protected override void OnHide()
        {
            if (_profileManager != null)
            {
                _profileManager.OnStateChanged -= UpdateSaveButtonState;
                _profileManager.UnregisterView();
            }
        }

        public void OnClickClose()
        {
            if (_profileManager != null && _profileManager.HasChanges()) 
            {
                _popup?.ShowPopup("PopupProfileMessage");
            }
            else
            {
                OnClose();
            }
        }

        public void SaveChangesAndClose()
        {
            if (_profileManager != null)
            {
                _profileManager.HandleSaveRequested(); 
            }
            OnClose();
        }

        private void UpdateSaveButtonState()
        {
            if (saveButton != null && _profileManager != null)
            {
                bool isChanged = _profileManager.HasChanges();
                saveButton.interactable = isChanged;
            }
        }

        protected override bool CheckAutoShow() => false;
        protected override void Unload() { }
    }
}