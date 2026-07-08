using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    public class BonusBankMilestoneUiItem : MonoBehaviour
    {
        [SerializeField] private Button btnClaim;
        [SerializeField] private GameObject lockContainer;
        [SerializeField] private GameObject unLockContainer;
        [SerializeField] private GameObject objLocked;
        [SerializeField] private GameObject objClaimed;
        [SerializeField] private GamePassExpSlider amountSlider;
    }
}
