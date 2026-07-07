using ChieChie.Constracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChieChie.Core
{
    public class ItemRewardInfoSlotView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text resourceIdText;
        [SerializeField] private GameObject infiniteRewardVisual;
        [SerializeField] private bool showResourceId;
        [SerializeField] private bool showAmountPrefix = true;

        public void Setup(IItemReward reward)
        {
            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (iconImage != null)
            {
                Sprite rewardIcon = ItemRewardInfoFormatter.GetRewardIcon(reward);
                iconImage.sprite = rewardIcon;
                iconImage.enabled = rewardIcon != null;
            }

            if (amountText != null)
            {
                amountText.text = ItemRewardInfoFormatter.GetAmountText(reward, showAmountPrefix);
            }

            if (resourceIdText != null)
            {
                bool shouldShowResourceId = showResourceId && !string.IsNullOrEmpty(reward.ResourceId);
                resourceIdText.gameObject.SetActive(shouldShowResourceId);
                if (shouldShowResourceId)
                {
                    resourceIdText.text = reward.ResourceId;
                }
            }

            if (infiniteRewardVisual != null)
            {
                infiniteRewardVisual.SetActive(reward.IsInfiniteReward);
            }
        }
    }
}
