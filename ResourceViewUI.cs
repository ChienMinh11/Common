using System;
using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.MVP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class ResourceViewUI : BaseView<IResourcePresenter>, IResourceView
    {
        [SerializeField] private ResourceIdentity resourceId;

        [Header("Standard Resource UI")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Optional Resource Widgets")]
        [SerializeField] private UIResourceInfinityWidget infinityWidget;
        [SerializeField] private UIResourceRegenWidget regenWidget;

        [Header("Infinite Resource UI Fallback")]
        [SerializeField] private GameObject infiniteBadge;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Animation Settings")]
        [SerializeField] private NumberAnimationSettings animationSettings;

        private NumberTextAnimation _numberAnimation;
        private long _currentDisplayedAmount;
        private bool _isInfiniteActive;

        public string ResourceKey => resourceId != null ? resourceId.ResourceId : string.Empty;

        [Inject]
        private void Construct(
            IPresenterFactory<IResourcePresenter, IResourceView> presenterFactory)
        {
            if (Presenter == null)
            {
                BindPresenter(presenterFactory, this);
            }
        }

        public void SetResourceAmount(long amount)
        {
            if (amountText == null) return;

            EnsureAnimationInitialized();
            _numberAnimation.AnimateTo(_currentDisplayedAmount, amount);
        }

        public void SetResourceAmountWithoutAnimation(long amount)
        {
            UpdateAmountText(amount);
        }

        public void SetResourceIcon(Sprite icon)
        {
            if (iconImage != null && iconImage.sprite != icon)
            {
                iconImage.sprite = icon;
            }
        }

        public void SetResourceName(string resourceName)
        {
            if (nameText != null && nameText.text != resourceName)
            {
                nameText.text = resourceName;
            }
        }

        public void ShowInsufficientMessage()
        {
            Debug.LogWarning($"[{name}] Not enough {ResourceKey}.");
        }

        public void OnMaxStackReached(string resourceKey)
        {
            Debug.Log($"[{name}] {resourceKey} reached its maximum stack.");
        }

        public void UpdateInfinityStatus(bool isInfinite, DateTime expirationTime)
        {
            _isInfiniteActive = isInfinite;

            if (amountText != null)
            {
                amountText.gameObject.SetActive(!isInfinite);
            }

            if (infinityWidget != null)
            {
                infinityWidget.Setup(isInfinite, expirationTime);
            }

            SetInfiniteFallback(isInfinite, expirationTime);
        }

        public void UpdateRegenStatus(
            bool isRegenEnabled,
            bool isMaxStack,
            DateTime nextRegenTime)
        {
            if (regenWidget == null) return;

            if (_isInfiniteActive)
            {
                regenWidget.Setup(false, false, DateTime.MinValue);
                return;
            }

            regenWidget.Setup(isRegenEnabled, isMaxStack, nextRegenTime);
        }

        protected override void OnDestroy()
        {
            _numberAnimation?.Stop();
            base.OnDestroy();
        }

        private void EnsureAnimationInitialized()
        {
            if (_numberAnimation != null) return;

            animationSettings ??= new NumberAnimationSettings();
            _numberAnimation = new NumberTextAnimation(animationSettings, UpdateAmountText);
        }

        private void UpdateAmountText(long amount)
        {
            _currentDisplayedAmount = amount;
            if (amountText != null)
            {
                amountText.text = NumberFormatter.FormatNumber(amount);
            }
        }

        private void SetInfiniteFallback(bool isInfinite, DateTime expirationTime)
        {
            if (infiniteBadge != null)
            {
                infiniteBadge.SetActive(isInfinite);
            }

            if (countdownText == null) return;

            if (!isInfinite || expirationTime == DateTime.MinValue)
            {
                countdownText.text = string.Empty;
                return;
            }

            TimeSpan remaining = expirationTime - DateTime.UtcNow;
            countdownText.text = FormatRemainingTime(remaining);
        }

        private static string FormatRemainingTime(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return string.Empty;

            int totalHours = (int)remaining.TotalHours;
            return totalHours > 0
                ? $"{totalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}
