using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class PopupProfileInputText : PopupBase
    {
        [SerializeField] private TextMeshProUGUI tittle;
        [SerializeField] private TMP_InputField inputText;
       
        private Action<string> _onContinueCallback;
        protected override void SetPopupName() => PopupName = "PopupProfileInputText";
       

        protected override void SetCacheable() => IsCache = false;

  

        protected override void OnShow()
        {
          
        }

        protected override void OnHide()
        {
            
        }

        protected override bool CheckAutoShow()
        {
           return false;
        }

        protected override void Unload()
        {
           
        }
        public void SetTittleText(string text = "")
        {
            if (tittle != null) tittle.text = text.ToString();
        }
        
        public void SetInputText(string text = "")
        {
            if (inputText != null) inputText.text = text;
        }
        
        public void SetContinueCallback(Action<string> callback)
        {
            _onContinueCallback = callback;
        }
        
        public void OnComfirmButtonClicked()
        {
            if (inputText != null)
            {
                string newValue = inputText.text;
                _onContinueCallback?.Invoke(newValue);
            }
            
            OnClose();
        }

        public void OnCancelClick()
        {
            OnClose();
        }
        
    }
}
