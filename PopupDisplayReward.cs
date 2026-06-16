using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
    public class PopupDisplayReward : PopupBase
    {
       [Header("UI References")]
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private Transform rewardGridContainer;
        [SerializeField] private RewardSlotView rewardSlotPrefab;

        private RewardDisplayService _rewardDisplayService;
        private IIconProvider _iconProvider; // Inject ResourceManager của Core
        private readonly List<GameObject> _spawnedSlots = new List<GameObject>();

        [Inject]
        public void Construct(RewardDisplayService rewardDisplayService, IIconProvider iconProvider)
        {
            _rewardDisplayService = rewardDisplayService;
            _iconProvider = iconProvider;
        }

        protected override void SetPopupName() => PopupName = "PopupDisplayReward";
        protected override void SetCacheable() => IsCache = false;

        protected override bool CheckAutoShow()
        {
            return false;
        }

        protected override void OnShow()
        {
            foreach (var slot in _spawnedSlots) if (slot != null) Destroy(slot);
            _spawnedSlots.Clear();

            var data = _rewardDisplayService.CurrentData;
            if (data == null) return;

            if (txtTitle != null) txtTitle.text = data.GetTitle();
            if (txtDescription != null) txtDescription.text = data.GetDescription();


            foreach (var reward in data.GetRewards())
            {
                if (rewardSlotPrefab != null && rewardGridContainer != null)
                {
                    RewardSlotView slotUI = Instantiate(rewardSlotPrefab, rewardGridContainer);
                  
                    Sprite rewardSprite = null;
                    rewardSprite = reward.isInfiniteReward
                        ? GetIconResourceReward(reward.resourceType, true)
                        : GetIconResourceReward(reward.resourceType, false);
                    
                    slotUI.Setup(reward, rewardSprite);
                    slotUI.gameObject.SetActive(true);
                    _spawnedSlots.Add(slotUI.gameObject);
                }
            }
        }

        private Sprite GetIconResourceReward(ResourceType resourceType, bool isInfinite)
        {
            if (_iconProvider == null) return null;
        
            return _iconProvider.GetRewardIcon(resourceType, isInfinite);
        }

        protected override void OnHide()
        {
            _rewardDisplayService.CurrentData?.OnClosePopup();
            _rewardDisplayService.SetContextData(null); 
        }

        public void OnClickClaimAndClose()
        {
            _rewardDisplayService.CurrentData?.OnRewardsClaimed();
            OnClose();
        }
    }
}
