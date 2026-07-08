using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class ShopItemView : MonoBehaviour
    {
        [Header("Identify Product")]
        [Tooltip("Chọn đúng ProductID của vật phẩm này trong Editor")]
        [SerializeField] private ProductIdentity targetProductID;
        public string TargetProductID => targetProductID.productID;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI txtPackageName;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private Image imgIcon;
        
        [Tooltip("Kéo thả component UITimeCountdownWidget của Pack này vào đây")]
        [SerializeField] private UITimeCountdownWidget countdownWidget; // Gắn Time UI vào đây

        [Header("Static Rewards UI Slots (Sắp xếp sẵn ở Editor)")]
        [Tooltip("Kéo thả các Slot Reward đã sắp sẵn vị trí ở đây")]
        [SerializeField] private List<RewardSlotView> staticRewardSlots = new List<RewardSlotView>();

        private Action<string> _onBuyClicked;

        public void OnClick()
        {
            _onBuyClicked?.Invoke(targetProductID.productID);
        }
    
        public void Setup(IShopItemData itemData, IShopService shopService, Action<string> onBuyClicked)
        {
            
            targetProductID.productID = itemData.ProductID; 
            _onBuyClicked = onBuyClicked;

            if (txtPackageName != null) txtPackageName.text = itemData.DisplayName; 
            if (txtDescription != null) txtDescription.text = itemData.Description; 
            if (imgIcon != null) imgIcon.sprite = itemData.Icon; 

            SetupStaticRewards(itemData.Rewards, shopService);
            
            if (countdownWidget != null)
            {
               
                if (shopService != null && shopService.TryGetOfferTimeRemaining(itemData.ProductID, out TimeSpan remaining))
                {
                    countdownWidget.gameObject.SetActive(true);
                    countdownWidget.Setup(remaining); 
                }
                else
                {
                    countdownWidget.gameObject.SetActive(false);
                }
            }
          
            if (txtPrice != null && txtPrice.text == "Loading...") txtPrice.text = "Loading...";
        }

        private void SetupStaticRewards(IReadOnlyList<IItemReward> rewards, IShopService shopService)
        {
            if (staticRewardSlots == null || staticRewardSlots.Count == 0) return;

            for (int i = 0; i < staticRewardSlots.Count; i++)
            {
                var slot = staticRewardSlots[i];
                if (slot == null) continue;

                if (rewards != null && i < rewards.Count)
                {
                    var rewardData = rewards[i];
              
                    slot.Setup(rewardData);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        public void UpdatePriceText(string localizedPrice)
        {
            if (txtPrice != null) txtPrice.text = localizedPrice; //
        }
    }
}