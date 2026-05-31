using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

namespace ChieChie.Core
{
    public class ShopItemView : MonoBehaviour
    {
        [Header("Identify Product")]
        [Tooltip("Chọn đúng ProductID của vật phẩm này trong Editor")]
        [SerializeField] private ProductID targetProductID;
        public ProductID TargetProductID => targetProductID;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI txtPackageName;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private Button btnBuy;
        [SerializeField] private Image imgIcon;

        [Header("Static Rewards UI Slots (Sắp xếp sẵn ở Editor)")]
        [Tooltip("Kéo thả các Slot Reward đã sắp sẵn vị trí ở đây")]
        [SerializeField] private List<RewardSlotView> staticRewardSlots = new List<RewardSlotView>();

        private Action<ProductID> _onBuyClicked;

        private void Awake()
        {
            if (btnBuy != null)
            {
                btnBuy.onClick.AddListener(() => _onBuyClicked?.Invoke(targetProductID)); 
            }
        }
    
        public void Setup(ShopItemData itemData, ShopPresenter presenter, Action<ProductID> onBuyClicked)
        {
            targetProductID = itemData.productID; 
            _onBuyClicked = onBuyClicked;

            if (txtPackageName != null) txtPackageName.text = itemData.displayName; 
            if (txtDescription != null) txtDescription.text = itemData.description; 
            if (imgIcon != null) imgIcon.sprite = itemData.icon; 

            SetupStaticRewards(itemData.rewards, presenter);
          
            if (txtPrice != null && txtPrice.text == "Loading...") txtPrice.text = "Loading...";
        }

        private void SetupStaticRewards(List<ShopItemReward> rewards, ShopPresenter presenter)
        {
            if (staticRewardSlots == null || staticRewardSlots.Count == 0) return;

            for (int i = 0; i < staticRewardSlots.Count; i++)
            {
                var slot = staticRewardSlots[i];
                if (slot == null) continue;

                if (rewards != null && i < rewards.Count)
                {
                    var rewardData = rewards[i];
                 
                    Sprite rewardSprite = presenter != null 
                        ? presenter.GetIconResourceReward(rewardData.resourceType, rewardData.isInfiniteReward) 
                        : null;

                    slot.Setup(rewardData, rewardSprite);
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