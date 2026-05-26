using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace MyFramework
{
    [RequireComponent(typeof(ResourceViewInitializer))]
    public class ResourceView : MonoBehaviour, IResourceView
    {
         [SerializeField] protected TextMeshProUGUI amountText;
        [SerializeField] protected TextMeshProUGUI nameText;
        [SerializeField] protected Image iconImage;
        [SerializeField] protected GameObject insufficientPanel;
        [SerializeField] protected GameObject amountPanel;
        [SerializeField] protected GameObject iconShop;
        [SerializeField] protected bool canHideBtnShop = true;
        [SerializeField] protected ActionScaleYoyo iconScale;
        [SerializeField] protected ActionPlaySound playSound;
        [SerializeField] protected ActionSpawnParticleInUI actionSpawnParticleInUI;
        
        [Header("Infinite Resource UI")]
        [SerializeField] private TextMeshProUGUI infiniteTimeText;
        [SerializeField] private GameObject[] infiniteTimePanel;
        [SerializeField] private bool showHoursInTimer = true;
        
        [Header("Number Format Settings")]
        [SerializeField] private int minDecimals = 0;
        [SerializeField] private int maxDecimals = 2;
        [SerializeField] private bool useSimpleFormat = true;
        
        [Header("Text Animation Settings")]
        [SerializeField] private NumberAnimationSettings animationSettings = new NumberAnimationSettings();
        [SerializeField] private bool enableTextAnimation = true;

        private NumberAnimation textAnimation;
        private long currentDisplayAmount;
        private int animationTimerHash;

        private Sprite normalIcon;
        private Sprite infiniteIcon;
        protected TimeManager timeManager;
        protected IEventService eventService;
        protected ResourcePresenterFactory factory;
        protected AudioManager audioManager;
        protected bool isInfiniteActive;
        public Image IconImage => iconImage;

        protected virtual void Awake() {}
        public virtual void Init(ResourceType resourceType,IEventService eventService, TimeManager timeManager,ResourcePresenterFactory factory)
        {
           // if(iconScale == null) Debug.LogError($"({gameObject.name}) =>iconScale is null");
            //if(playSound == null) Debug.LogError($"({gameObject.name}) =>playSound is null");
            if(actionSpawnParticleInUI != null) actionSpawnParticleInUI.InitValue();
            this.eventService = eventService;
            this.factory = factory;
            this.timeManager = timeManager;
            audioManager = ServiceLocator.GetService<AudioManager>();
            
            animationTimerHash = $"ResourceAnim_{resourceType}_{GetInstanceID()}".GetHashCode();
    
            if (enableTextAnimation)
            {
                textAnimation = new NumberAnimation(
                    animationSettings,
                    timeManager,
                    OnAnimationValueChanged,
                    animationTimerHash
                );
            }
    
            if (insufficientPanel != null)
                insufficientPanel.SetActive(false);
        
            // if (infiniteTimePanel != null)
            //     infiniteTimePanel.SetActive(false);
            ActiveInfinityPanel(false);
                
            UpdateIconShop(resourceType, factory);
     
            eventService.Subscribe<MoveCompletedMessage, SystemEventType>(
                SystemEventType.OnObjecMoveCompleted, 
                (message =>
                {
                    var targetResourceComponent = message.Target;
                    if (targetResourceComponent == iconImage.transform)
                    {
                        factory.ProcessPendingUpdates(resourceType);
                        iconScale?.StartAction();
                        playSound?.StartAction();
                        if(actionSpawnParticleInUI != null) actionSpawnParticleInUI.StartAction();
                    }
                })
            );
           
        }
     
        private void OnAnimationValueChanged(long animatedValue)
        {
            if (amountText != null && !isInfiniteActive)
            {
                string formattedAmount = useSimpleFormat 
                    ? NumberFormatter.FormatNumber(animatedValue)
                    : NumberFormatter.FormatNumberWithPrecision(animatedValue, minDecimals, maxDecimals);
                amountText.text = formattedAmount;
            }
        }

        private void UpdateIconShop(ResourceType resourceType, ResourcePresenterFactory factory)
        {
            if (factory != null)
            {
                long currentAmount = factory.GetCurrentAmount(resourceType);
               
                if (currentAmount <= 0)
                {
                    if (amountPanel != null)
                        amountPanel.SetActive(false);
                    if (iconShop != null)
                        iconShop.SetActive(true);
                }
                else
                {
                    if (amountPanel != null)
                        amountPanel.SetActive(true);
                    if (iconShop != null)
                        iconShop.SetActive(false);
                }
               
                if (factory.IsInfiniteResource(resourceType))
                {
                    if (iconShop != null) iconShop.SetActive(false);
                       
                   if(amountPanel!=null)amountPanel.SetActive(false);
                }
            }
        }

        public void SetResourceAmount<T>(T amount)
        {
            if (amountText != null)
            {
                if (isInfiniteActive)
                {
                    amountText.gameObject.SetActive(false);
                    if (amountPanel != null)
                        amountPanel.SetActive(false);
                    return;
                }

                string amountStr = amount.ToString();
                if (long.TryParse(amountStr, out long longAmount))
                {
                    // Xử lý hiển thị UI elements
                    if (longAmount <= 0)
                    {
                        if (canHideBtnShop)
                        {
                            if (amountPanel != null)
                                amountPanel.SetActive(false);
                            if (iconShop != null)
                                iconShop.SetActive(true);
                        }
                    }
                    else
                    {
                        if (canHideBtnShop)
                        {
                            if (amountText != null)
                                amountText.gameObject.SetActive(true);
                            if (amountPanel != null)
                                amountPanel.SetActive(true);
                            if (iconShop != null)
                                iconShop.SetActive(false);
                        }
                    }

                    // Animate text nếu có thay đổi
                    if (enableTextAnimation && textAnimation != null && currentDisplayAmount != longAmount)
                    {
                        textAnimation.AnimateTo(currentDisplayAmount, longAmount);
                        currentDisplayAmount = longAmount;
                    }
                    else
                    {
                        // Không có animation, cập nhật trực tiếp
                        currentDisplayAmount = longAmount;
                        OnAnimationValueChanged(longAmount);
                    }
                }
            }
        }

        public void SetResourceAmountWithoutAnimation<T>(T amount)
        {
            if (amountText != null)
            {
                if (isInfiniteActive)
                {
                    amountText.gameObject.SetActive(false);
                    if (amountPanel != null)
                        amountPanel.SetActive(false);
                    return;
                }

                string amountStr = amount.ToString();
                if (long.TryParse(amountStr, out long longAmount))
                {
                    // Xử lý hiển thị UI elements (giống SetResourceAmount)
                    if (longAmount <= 0)
                    {
                        if (canHideBtnShop)
                        {
                            if (amountPanel != null)
                                amountPanel.SetActive(false);
                            if (iconShop != null)
                                iconShop.SetActive(true);
                        }
                    }
                    else
                    {
                        if (canHideBtnShop)
                        {
                            if (amountText != null)
                                amountText.gameObject.SetActive(true);
                            if (amountPanel != null)
                                amountPanel.SetActive(true);
                            if (iconShop != null)
                                iconShop.SetActive(false);
                        }
                    }

                    // Cập nhật text trực tiếp mà không animation
                    currentDisplayAmount = longAmount;
                    OnAnimationValueChanged(longAmount);
                }
            }
        }

        public void SetResourceIcon(Sprite icon)
        {
            if (iconImage != null)
            {
                normalIcon = icon;
                iconImage.sprite = icon;
            }
        }

        public void SetInfiniteState(bool isInfinite)
        {
            isInfiniteActive = isInfinite;
            
            if (iconImage != null && infiniteIcon != null && normalIcon != null)
            {
                iconImage.sprite = isInfinite ? infiniteIcon : normalIcon;
            }
            // if (infiniteTimePanel != null)
            // {
            //     infiniteTimePanel.SetActive(isInfinite);
            //     if (infiniteTimeText != null)
            //     {
            //         infiniteTimeText.gameObject.SetActive(isInfinite);
            //     }
            // }
            ActiveInfinityPanel(isInfinite);
            if (amountText != null)
            {
                amountText.gameObject.SetActive(!isInfinite);
                SetOther(!isInfinite);
            }
            if (isInfinite && iconShop != null)
            {
                iconShop.SetActive(false);
            }
        }

        protected virtual void SetOther(bool isInfinite)
        {
          
        }

        public void SetInfiniteIcon(Sprite icon)
        {
            infiniteIcon = icon;
        }

        public void UpdateInfiniteTimeRemaining(float remainingTime)
        {
            if (infiniteTimeText != null)
            {
                if (remainingTime <= 0)
                {
                    infiniteTimeText.text = "";
                    // if (infiniteTimePanel != null)
                    //     infiniteTimePanel.SetActive(false);
                    ActiveInfinityPanel(false);

                    if (amountText != null)
                    {
                        amountText.gameObject.SetActive(true);
                    }
                    isInfiniteActive = false;
                    return;
                }

                string timeString = timeManager.FormatTime(remainingTime);
                infiniteTimeText.text = timeString;

                // if (infiniteTimePanel != null)
                // {
                //     infiniteTimePanel.SetActive(true);
                //     infiniteTimeText.gameObject.SetActive(true);
                // }
                ActiveInfinityPanel(true);
            }
        }

        public void OnMaxStackReached(ResourceType type)
        {
            SetOnMaxStackReached(type);
        }
        protected virtual void  SetOnMaxStackReached(ResourceType type){}


        public void SetResourceName(string name)
        {
            if (nameText != null)
                nameText.text = name;
        }

        public void ShowInsufficientMessage()
        {
            if (insufficientPanel != null)
            {
                insufficientPanel.SetActive(true);
                Invoke(nameof(HideInsufficientMessage), 2f);
            }
        }

        private void HideInsufficientMessage()
        {
            if (insufficientPanel != null)
                insufficientPanel.SetActive(false);
        }
        private void OnDestroy()
        {
            textAnimation?.Stop();
        }

        void ActiveInfinityPanel(bool value)
        {
            foreach (var panel in infiniteTimePanel)
            {
                if(panel != null) panel.SetActive(value);
                
            }
            if(infiniteTimeText!=null) infiniteTimeText.gameObject.SetActive(value);
        }
    }
}