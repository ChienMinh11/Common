using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChieChie.Core
{
    public class RewardSlotView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image imgRewardIcon;
        [SerializeField] private TextMeshProUGUI txtRewardAmount;
        
        public void Setup(BaseRewardData rewardData, Sprite rewardSprite)
        {
            if (rewardData == null) return;
            Setup(rewardData.isInfiniteReward, rewardData.amount, rewardData.infinityDuration, rewardSprite);
        }

        public void Setup(bool isInfinite, long amount, float duration, Sprite rewardSprite)
        {
            if (imgRewardIcon != null) 
            {
                imgRewardIcon.sprite = rewardSprite;
                imgRewardIcon.gameObject.SetActive(rewardSprite != null);
            }

            if (txtRewardAmount != null)
            {
                if (isInfinite)
                {
             
                    txtRewardAmount.text = TimeFormatter.FormatTime(duration);
                }
                else
                {
                    txtRewardAmount.text = $"x{amount}"; 
                }
            }
        }
    }
}
