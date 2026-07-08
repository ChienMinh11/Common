using System;
using System.Collections.Generic;
using System.Text;
using ChieChie.Constracts;
using ChieChie.Shop;
using Cysharp.Threading.Tasks;
using Game.GamePlay;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace ChieChie.Core
{
    public class ShopView : MonoBehaviour, IShopView
    {
        [Header("Static UI Setup")]
        [Tooltip("Kéo thả tất cả các ShopItemView đã sắp xếp sẵn trong các Layout/Tab ở Editor vào đây")]
        [SerializeField]
        private List<ShopItemView> staticItemViews = new List<ShopItemView>();

        [Header("Loading UI")] [SerializeField]
        private GameObject loadingOverlay;
        private IShopService _shopService;
        private readonly Dictionary<string, ShopItemView> _activeItems = new Dictionary<string, ShopItemView>();
        private readonly Dictionary<string, IShopItemData> _itemDataCache = new Dictionary<string, IShopItemData>();
        private Action<string> _buyItemCallback;

        [Inject]
        public void Construct(IShopService shopService)
        {
            _shopService = shopService;
            Initialize();
        }

        private void Initialize()
        {
            if (_shopService != null)
            {
                _shopService.RegisterView(this);
            }
            else
            {
                Debug.LogError("[ShopView] Không tìm thấy ShopPresenter từ ShopManager!");
            }
        }

        private void OnDestroy()
        {
            if (_shopService != null)
            {
                _shopService.UnregisterView(this);
            }
        }

        public void Initialize(IReadOnlyList<IShopItemData> items)
        {
            _activeItems.Clear();
            _itemDataCache.Clear();

            foreach (var itemData in items)
            {
                if (itemData.ProductID == "") continue;

                if (!_itemDataCache.ContainsKey(itemData.ProductID))
                {
                    _itemDataCache.Add(itemData.ProductID, itemData);
                }
            }

            foreach (var itemUI in staticItemViews)
            {
                if (itemUI == null) continue;
                var id = itemUI.TargetProductID;

                if (_itemDataCache.TryGetValue(id, out var realData))
                {
                    _activeItems[id] = itemUI;
                    bool isOwned = _shopService != null && _shopService.IsItemOwned(id);
                    itemUI.gameObject.SetActive(!isOwned);
                }
                else
                {
                    itemUI.gameObject.SetActive(false);
                }
            }
        }

        public void SetBuyItemCallback(Action<string> callback)
        {
            _buyItemCallback = callback;

            foreach (var kvp in _activeItems)
            {
                var productId = kvp.Key;
                var itemUI = kvp.Value;

                if (_itemDataCache.TryGetValue(productId, out var realData))
                {
                    itemUI.Setup(realData, _shopService, _buyItemCallback);
                }
            }
        }

        public void UpdatePrice(string itemId, string price)
        {
            if (_activeItems.TryGetValue(itemId, out var itemUI))
            {
                itemUI.UpdatePriceText(price);
            }
        }

        public void OnPurchaseSuccess(string itemId)
        {
            if (!gameObject.activeInHierarchy) return; 

            var itemData = GetItemData(itemId);

            if (itemData != null && (itemData.IsOneTimePurchase || itemData.IsTimeLimited))
            {
                if (_activeItems.TryGetValue(itemId, out var itemUI))
                {
                    itemUI.gameObject.SetActive(false);
                }
            }
        }
        
        public void OnPurchaseFailed(string itemId, string reason)
        {
            
        }

        public void ShowLoadingIndicator(bool show)
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(show);
            }
        }

        
        private IShopItemData GetItemData(string id)
        {
            return _itemDataCache.TryGetValue(id, out var data) ? data : null;
        }

#if UNITY_EDITOR

        [Button("Find All Shop Items")]
        private void FindPack()
        {
            
            staticItemViews.Clear();
       
            var childItems = GetComponentsInChildren<ShopItemView>(true);

            staticItemViews.AddRange(childItems);

            UnityEditor.EditorUtility.SetDirty(this);
            
            Debug.Log($"[ShopView] Đã tìm thấy và thêm {staticItemViews.Count} ShopItemView vào danh sách!");
        }
#endif
    }
}