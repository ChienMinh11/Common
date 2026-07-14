using System;
using ChieChie.Constracts;
using ChieChie.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class ResourceViewUI : MonoBehaviour, IResourceView
    {
        [SerializeField] private ResourceIdentity resourceId;

        [Header("Standard Resource UI")] [SerializeField]
        private TextMeshProUGUI amountText;

        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Infinite Resource UI Add-on")] [SerializeField]
        private GameObject infiniteBadge;

        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Animation Settings")] [SerializeField]
        private NumberAnimationSettings animationSettings;

        private NumberTextAnimation numberAnimation;
        private long currentDisplayedAmount;

        private IResourceService _resourceService;

        [Inject]
        private void Construct(IResourceService resourceService)
        {
            _resourceService = resourceService;
            _resourceService.RegisterView(resourceId.ResourceId, this);
        }

        private void EnsureAnimationInitialized()
        {
            if (numberAnimation == null)
            {
                if (animationSettings == null)
                {
                    animationSettings = new NumberAnimationSettings();
                }

                numberAnimation = new NumberTextAnimation(animationSettings, UpdateAmountText);
            }
        }

        public void SetResourceAmount(long amount)
        {
            if (amountText == null) return;

            EnsureAnimationInitialized();
            long fromValue = currentDisplayedAmount;
            numberAnimation.AnimateTo(fromValue, amount);
        }

        public void SetResourceAmountWithoutAnimation(long amount)
        {
            UpdateAmountText(amount);
        }

        private void UpdateAmountText(long amount)
        {
            currentDisplayedAmount = amount;
            if (amountText != null)
            {
                amountText.text = NumberFormatter.FormatNumber(amount);
            }
        }

        public void SetResourceIcon(Sprite icon)
        {
            if (iconImage != null)
                iconImage.sprite = icon;
        }

        public void SetResourceName(string name)
        {
            if (nameText != null)
                nameText.text = name;
        }

        public void ShowInsufficientMessage()
        {
            Debug.LogWarning($"[{name}] Không đủ tài nguyên!");
        }

        public void OnMaxStackReached(string type)
        {
            Debug.Log($"[{name}] Đã đạt giới hạn stack của {type}!");
        }

        public void UpdateInfinityStatus(bool isInfinite, DateTime expirationTime)
        {
            
        }

        public void UpdateRegenStatus(bool isRegenEnabled, bool isMaxStack, DateTime nextRegenTime)
        {
            
        }

        // --- HIỆN THỰC IInfiniteResourceView ---
        public void SetInfiniteStatus(bool isInfinite)
        {
            if (infiniteBadge != null)
            {
                infiniteBadge.SetActive(isInfinite);
            }

            // Khi vô hạn thì ẩn text số lượng đi để giao diện gọn gàng
            if (amountText != null)
            {
                amountText.gameObject.SetActive(!isInfinite);
            }

            if (!isInfinite && countdownText != null)
            {
                countdownText.text = string.Empty;
            }
        }

        public void UpdateInfinityRemainingTime(string formattedTime)
        {
            if (countdownText != null)
            {
                countdownText.text = formattedTime;
            }
        }

        public void SetRegenStatusActive(bool isActive)
        {
           
        }

        public void SetRegenStatusText(string text)
        {
           
        }

        private void OnDestroy()
        {
            numberAnimation?.Stop();
            if (_resourceService != null)
            {
                _resourceService.UnregisterView(this);
            }
        }
    }
}
