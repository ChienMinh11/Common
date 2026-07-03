using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChieChie.Constracts;
using ChieChie.GamePass;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class GamePassView : MonoBehaviour, IPassView 
    {
        private const string COMPLETED = "Completed!";
        [Header("Top Bar")] 
        [SerializeField] private UITimeCountdownWidget timeCountdownWidget;
        [SerializeField] private TMP_Text txtCurrentExp;
        [SerializeField] private TMP_Text txtCurrentLevel;
        [SerializeField] private GamePassExpSlider expSlider;
        [SerializeField] private Button btnBuyPremium;
        [SerializeField] private GameObject objPremiumBadge;
        [SerializeField] private GameObject objBonusPassActiveVisual;

        [Header("Milestones List")] [SerializeField]
        private Transform milestoneContainer;
        [SerializeField] private Transform startIndex;
        [SerializeField] private Transform endIndex;

        [SerializeField] private MilestoneUIItem milestonePrefab;

        [Header("Bonus Milestones List")] 
        [SerializeField] private Transform bonusContainer;
        [SerializeField] private BonusMilestoneUIItem bonusPrefab;

        private readonly List<MilestoneUIItem> _milestonePool = new List<MilestoneUIItem>();
        private readonly List<BonusMilestoneUIItem> _bonusPool = new List<BonusMilestoneUIItem>();

        public event Action<int, bool> OnClaimRewardClicked;
        public event Action<int> OnClaimBonusClicked;
        public event Action OnBuyPremiumClicked;

        private readonly List<GameObject> _spawnedItems = new List<GameObject>();

        private IPassService _passService;

        

        public void Initialize(IPassService passService)
        {
            _passService = passService;
            _passService.RegisterView(this);
        }

        private void Awake()
        {
            btnBuyPremium.onClick.AddListener(() => OnBuyPremiumClicked?.Invoke());
        }

        private void OnDestroy()
        {
            _passService.UnregisterView(this);
        }


        public void RefreshUI(PassViewData viewData)
        {
            if (viewData == null) return;

            if (timeCountdownWidget != null)
            {
                timeCountdownWidget.Setup(viewData.EventEndTime);
            }

            if (txtCurrentLevel != null)
            {
                txtCurrentLevel.text = $"{viewData.CurrentMilestoneIndex}";
            }

            UpdateExpProgressUI(viewData);

            btnBuyPremium.gameObject.SetActive(!viewData.IsPremiumUnlocked);
            if (objPremiumBadge != null)
            {
                objPremiumBadge.SetActive(viewData.IsPremiumUnlocked);
            }

            if (viewData.Milestones != null)
            {
                var sortedMilestones = viewData.Milestones.OrderBy(m => m.Index).ToList();

                for (int i = 0; i < sortedMilestones.Count; i++)
                {
                    MilestoneUIItem item;
                    if (i < _milestonePool.Count)
                    {
                        item = _milestonePool[i];
                        item.gameObject.SetActive(true);
                    }
                    else
                    {
                        item = Instantiate(milestonePrefab, milestoneContainer);
                        _milestonePool.Add(item);
                    }
        
                    if (startIndex != null)
                    {
                        item.transform.SetSiblingIndex(startIndex.GetSiblingIndex() + i + 1);
                    }

                    item.Setup(sortedMilestones[i], HandleClaimRewardItem);
                }

                if (endIndex != null && startIndex != null)
                {
                    int nextIndexAfterMilestones = startIndex.GetSiblingIndex() + sortedMilestones.Count + 1;
                    endIndex.SetSiblingIndex(nextIndexAfterMilestones);
                }

                for (int i = sortedMilestones.Count; i < _milestonePool.Count; i++)
                {
                    _milestonePool[i].gameObject.SetActive(false);
                }
            }

            if (viewData.BonusMilestones != null)
            {
                var sortedBonus = viewData.BonusMilestones.OrderBy(b => b.Index).ToList(); //

                for (int i = 0; i < sortedBonus.Count; i++)
                {
                    BonusMilestoneUIItem item;
                    if (i < _bonusPool.Count)
                    {
                        item = _bonusPool[i];
                        item.gameObject.SetActive(true);
                    }
                    else
                    {
                        item = Instantiate(bonusPrefab, bonusContainer);
                        _bonusPool.Add(item);
                    }

                    item.Setup(sortedBonus[i], HandleClaimBonusItem);
                }

                for (int i = sortedBonus.Count; i < _bonusPool.Count; i++)
                {
                    _bonusPool[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateExpProgressUI(PassViewData viewData)
        {
            if (expSlider == null) return;
        
            bool isBonusActive = expSlider.UpdateProgressDetailed(viewData);

            if (txtCurrentLevel != null) 
            {
                txtCurrentLevel.gameObject.SetActive(!isBonusActive);
            }
    
            if (objBonusPassActiveVisual != null) 
            {
                objBonusPassActiveVisual.SetActive(isBonusActive);
            }
        }

        private void HandleClaimRewardItem(int index, bool isPremium)
        {
            OnClaimRewardClicked?.Invoke(index, isPremium);
        }

        private void HandleClaimBonusItem(int index)
        {
            OnClaimBonusClicked?.Invoke(index);
        }


        private void ClearOldItems()
        {
            foreach (var item in _spawnedItems)
            {
                if (item != null && item.transform != startIndex) 
                {
                    Destroy(item);
                }
            }
            _spawnedItems.Clear();
        }
    }
}