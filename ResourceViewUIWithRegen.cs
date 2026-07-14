using ChieChie.Constracts;
using ChieChie.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using System;

namespace Game.GamePlay
{
    public class ResourceViewUIWithRegen : MonoBehaviour, IResourceView
    {
        [SerializeField] private ResourceIdentity resourceId;
        
        [Header("Standard Resource UI")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Tối ưu hóa bằng Widget độc lập")]
        [SerializeField] private UIResourceInfinityWidget infinityWidget;
        [SerializeField] private UIResourceRegenWidget regenWidget;
        

        [Header("Animation Settings")]
        [SerializeField] private NumberAnimationSettings animationSettings;

        private NumberTextAnimation numberAnimation;
        private long currentDisplayedAmount;
        private IResourceService  _resourceService;
        private bool _isInfiniteActive = false;
       
        [Inject]
        private void Construct(IResourceService resourceService)
        { 
            _resourceService = resourceService;
            _resourceService.RegisterView(resourceId.ResourceId, this);
        }

        public void SetResourceAmount(long amount)
        {
            if (amountText == null) return;
            EnsureAnimationInitialized();
            numberAnimation.AnimateTo(currentDisplayedAmount, amount);
        }

        public void SetResourceAmountWithoutAnimation(long amount) => UpdateAmountText(amount);

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
            if (iconImage != null && iconImage.sprite != icon) iconImage.sprite = icon;
        }

        public void SetResourceName(string name)
        {
            if (nameText != null && nameText.text != name) nameText.text = name;
        }

        public void UpdateInfinityStatus(bool isInfinite, DateTime expirationTime)
        {
            if (_isInfiniteActive != isInfinite)
            {
                _isInfiniteActive = isInfinite;
                if (amountText != null) amountText.gameObject.SetActive(!isInfinite);
            }

            if (infinityWidget != null)
            {
                infinityWidget.Setup(isInfinite, expirationTime);
            }
        }

        public void UpdateRegenStatus(bool isRegenEnabled, bool isMaxStack, DateTime nextRegenTime)
        {
            // Nếu đang ở trạng thái vô hạn, tắt hiển thị thông báo hồi phục năng lượng hoàn toàn
            if (_isInfiniteActive)
            {
                if (regenWidget != null) regenWidget.Setup(false, false, DateTime.MinValue);
                return;
            }

            if (regenWidget != null)
            {
                regenWidget.Setup(isRegenEnabled, isMaxStack, nextRegenTime);
            }
        }
       

        public void ShowInsufficientMessage() => Debug.LogWarning($"[{name}] Không đủ tài nguyên!");
        public void OnMaxStackReached(string type) => Debug.Log($"[{name}] Đã đạt giới hạn stack của {type}!");

        private void EnsureAnimationInitialized()
        {
            if (numberAnimation == null)
            {
                if (animationSettings == null) animationSettings = new NumberAnimationSettings();
                numberAnimation = new NumberTextAnimation(animationSettings, UpdateAmountText);
            }
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
