using System;
using ChieChie.Booster;
using ChieChie.Constracts;
using ChieChie.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class ButtonUseBooster : MonoBehaviour, IResourceView
    {
        [Header("Settings")] 
        [SerializeField] private ResourceIdentity identitySource;

        private string BoosterType => identitySource.ResourceId;
        
        [Header("Visual Settings")] [SerializeField]
        private GameObject selectHighlight;

        [Header("Standard Resource UI")] [SerializeField]
        private TextMeshProUGUI amountText;

        [SerializeField] private Image iconImage;

        [Header("Infinite Resource UI Add-on")] [SerializeField]
        private GameObject infiniteBadge;

        [SerializeField] private TextMeshProUGUI countdownText;

        private IBoosterService _boosterController;
        private IResourceService _resourceService;
        private Button _button;
        private BoosterBehavior _myBehavior;
        private bool _isProcessingClick;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        [Inject]
        private void Contruct(IBoosterService boosterController, IResourceService resourceService)
        {
            _boosterController = boosterController;
            _resourceService = resourceService;
            _resourceService.RegisterView(BoosterType, this);
        }

        private void Start()
        {
            RegisterEvents();
            RefreshVisual();
        }

        private void OnEnable()
        {
            RegisterEvents();
            RefreshVisual();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            _resourceService?.UnregisterView(this);
        }

        private void RegisterEvents()
        {
            if (_boosterController == null) return;

            _myBehavior = _boosterController.GetBoosterBehavior(BoosterType);
            _boosterController.OnAwaitingStatusChanged += UpdateButtonInteractivity;
            _boosterController.OnPreBoosterStateChanged += OnPreBoosterStateChanged;
    
            _boosterController.OnBoosterInfinitePassConsumed += OnBoosterInfinitePassConsumed;
        }

        private void UnregisterEvents()
        {
            if (_boosterController == null) return;
            _boosterController.OnAwaitingStatusChanged -= UpdateButtonInteractivity;
            _boosterController.OnPreBoosterStateChanged -= OnPreBoosterStateChanged;
            _boosterController.OnBoosterInfinitePassConsumed -= OnBoosterInfinitePassConsumed;
        }

        private void OnBoosterInfinitePassConsumed(string changedType)
        {
            if (changedType == BoosterType)
            {
                long currentAmount = _resourceService.GetCurrentAmount(BoosterType);
                SetResourceAmount(currentAmount);

                if (amountText != null) amountText.gameObject.SetActive(true);
                if (countdownText != null) countdownText.text = string.Empty;
            }
        }

        private void OnPreBoosterStateChanged(string changedType)
        {
            if (changedType == BoosterType) RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (_myBehavior == null || _resourceService == null || _isProcessingClick) return;

           
            if (_myBehavior.Settings.BoosterType != ChieChie.Booster.BoosterType.PreBooster)
            {
                UpdateVisualState(false);
                return;
            }
            bool isInfinite = _resourceService.IsCurrentlyInfinite(BoosterType);

            if (isInfinite)
            {
                _button.interactable = false; 
                UpdateVisualState(true);
            }
            else
            {
                _button.interactable = true;
                UpdateVisualState(_myBehavior.IsSelected);
            }
        }

        private void UpdateVisualState(bool isSelected)
        {
            if (selectHighlight != null) selectHighlight.SetActive(isSelected);
        }

        private void UpdateButtonInteractivity(string currentAwaitingBoosterType)
        {
            if (_isProcessingClick) return;
            if (_myBehavior != null && _myBehavior.Settings.BoosterType == ChieChie.Booster.BoosterType.PreBooster) return;

            if (currentAwaitingBoosterType == null)
            {
                _button.interactable = true;
            }
            else
            {
                _button.interactable = (currentAwaitingBoosterType == BoosterType);
            }
        }

        public void OnButtonClick()
        {
            if (_isProcessingClick) return;
            ExecuteClickAsync().Forget();
        }

        private async UniTaskVoid ExecuteClickAsync()
        {
            _isProcessingClick = true;
            if(_myBehavior.Settings.BoosterType == ChieChie.Booster.BoosterType.PreBooster) _button.interactable = false;

            try
            {
                await _boosterController.UseBooster(BoosterType, this.GetCancellationTokenOnDestroy());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isProcessingClick = false;
                RefreshVisual();
            }
        }

        #region ResourceViewUI

        public void SetResourceAmount(long amount)
        {
            UpdateAmountText(amount);
        }

        public void SetResourceAmountWithoutAnimation(long amount)
        {
            UpdateAmountText(amount);
        }

        public void SetResourceIcon(Sprite icon)
        {
            if (iconImage != null)
                iconImage.sprite = icon;
        }

        public void SetResourceName(string name)
        {
        }

        public void ShowInsufficientMessage()
        {
        }

        public void OnMaxStackReached(string type)
        {
        }

        public void UpdateInfinityStatus(bool isInfinite, DateTime expirationTime)
        {
            
        }

        public void UpdateRegenStatus(bool isRegenEnabled, bool isMaxStack, DateTime nextRegenTime)
        {
            throw new NotImplementedException();
        }

        public void SetInfiniteStatus(bool isInfinite)
        {
            if (_myBehavior != null && 
                _myBehavior.Settings.BoosterType != ChieChie.Booster.BoosterType.PreBooster && 
                _myBehavior.HasUsedInfiniteFreePass)
            {
                isInfinite = false;
            }

            if (amountText != null)
            {
                amountText.gameObject.SetActive(!isInfinite);
            }

            if (!isInfinite && countdownText != null)
            {
                countdownText.text = string.Empty;
                infiniteBadge.gameObject.SetActive(false);
            }
        }

        public void UpdateInfinityRemainingTime(string formattedTime)
        {
            if (_myBehavior != null && 
                _myBehavior.Settings.BoosterType != ChieChie.Booster.BoosterType.PreBooster && 
                _myBehavior.HasUsedInfiniteFreePass)
            {
                if (countdownText != null) countdownText.text = string.Empty;
                infiniteBadge.gameObject.SetActive(false);
                return;
            }

            if (countdownText != null)
            {
                infiniteBadge.gameObject.SetActive(true);
                countdownText.text = formattedTime;
            }
        }

        public void SetRegenStatusActive(bool isActive)
        {
           
        }

        public void SetRegenStatusText(string text)
        {
           
        }

        private void UpdateAmountText(long amount)
        {
            if (amountText != null)
            {
                amountText.text = NumberFormatter.FormatNumber(amount);
            }
        }

        #endregion
    }
}
