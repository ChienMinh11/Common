using System.Collections.Generic;
using System.Linq;
using ChieChie.Constracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChieChie.Core
{
    public class ItemRewardInfoPanel : MonoBehaviour, IItemRewardInfoView
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private ItemRewardInfoSlotView rewardSlotPrefab;
        [SerializeField] private GameObject rewardSeparatorPrefab;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private string defaultTitle = "Rewards";
        [SerializeField] private Vector2 anchoredOffset = new Vector2(0f, 90f);
        [SerializeField] private bool hideOnAwake = true;

        private readonly List<ItemRewardInfoSlotView> _slotPool = new List<ItemRewardInfoSlotView>();
        private readonly List<GameObject> _separatorPool = new List<GameObject>();

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (panelRect == null) panelRect = root.GetComponent<RectTransform>();
            if (rewardsContainer == null) rewardsContainer = transform;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (hideOnAwake)
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }

        public void ShowRewards(IEnumerable<IItemReward> rewards, Transform anchor = null, string title = "")
        {
            if (rewardSlotPrefab == null || rewardsContainer == null)
            {
                return;
            }

            var rewardList = rewards?.Where(reward => reward != null).ToList() ?? new List<IItemReward>();
            if (rewardList.Count == 0)
            {
                Hide();
                return;
            }

            if (root != null)
            {
                root.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;
            }

            BuildRewardList(rewardList);
            PositionToAnchor(anchor);
            RebuildLayout();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void BuildRewardList(IReadOnlyList<IItemReward> rewards)
        {
            int slotIndex = 0;
            int separatorIndex = 0;
            int siblingIndex = 0;

            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0 && rewardSeparatorPrefab != null)
                {
                    GameObject separator = GetSeparator(separatorIndex++);
                    separator.SetActive(true);
                    separator.transform.SetSiblingIndex(siblingIndex++);
                }

                ItemRewardInfoSlotView slot = GetSlot(slotIndex++);
                slot.gameObject.SetActive(true);
                slot.transform.SetSiblingIndex(siblingIndex++);
                slot.Setup(rewards[i]);
            }

            for (int i = slotIndex; i < _slotPool.Count; i++)
            {
                _slotPool[i].gameObject.SetActive(false);
            }

            for (int i = separatorIndex; i < _separatorPool.Count; i++)
            {
                _separatorPool[i].SetActive(false);
            }
        }

        private ItemRewardInfoSlotView GetSlot(int index)
        {
            while (_slotPool.Count <= index)
            {
                _slotPool.Add(Instantiate(rewardSlotPrefab, rewardsContainer));
            }

            return _slotPool[index];
        }

        private GameObject GetSeparator(int index)
        {
            while (_separatorPool.Count <= index)
            {
                _separatorPool.Add(Instantiate(rewardSeparatorPrefab, rewardsContainer));
            }

            return _separatorPool[index];
        }

        private void PositionToAnchor(Transform anchor)
        {
            if (anchor == null || panelRect == null || panelRect.parent == null) return;

            var parentRect = panelRect.parent as RectTransform;
            if (parentRect == null) return;

            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3 anchorWorldPosition = anchor.position;
            if (anchor is RectTransform anchorRect)
            {
                anchorWorldPosition = anchorRect.TransformPoint(anchorRect.rect.center);
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, anchorWorldPosition);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, eventCamera, out var localPoint))
            {
                panelRect.anchoredPosition = localPoint + anchoredOffset;
            }
        }

        private void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (rewardsContainer is RectTransform rewardsRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rewardsRect);
            }

            if (panelRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
