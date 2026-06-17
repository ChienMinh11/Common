using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopStorage
    {
        private readonly IShopSaveAdapter _saveAdapter;
        private readonly HashSet<ProductID> _oneTimePurchases = new HashSet<ProductID>();
        private readonly HashSet<ProductID> _timeLimitedPurchases = new HashSet<ProductID>();
        
        public ShopStorage(IShopSaveAdapter saveAdapter)
        {
            this._saveAdapter = saveAdapter;
            LoadOneTimePurchases();
            LoadTimeLimitedPurchases();
        }
        
        #region One-Time Purchases
        
        private void LoadOneTimePurchases()
        {
            var savedData = _saveAdapter.LoadOneTimePurchases();
            if (!string.IsNullOrEmpty(savedData))
            {
                var items = savedData.Split(',');
                foreach (var item in items)
                {
                    if (Enum.TryParse<ProductID>(item, out var productID) && productID != ProductID.NONE)
                    {
                        _oneTimePurchases.Add(productID);
                    }
                }
            }
        }
        
        public void SaveOneTimePurchases()
        {
            string saveData = string.Join(",", _oneTimePurchases);
            _saveAdapter.SaveOneTimePurchases(saveData);
        }
        
        public void AddOneTimePurchase(ProductID productId)
        {
            if (!_oneTimePurchases.Contains(productId))
            {
                _oneTimePurchases.Add(productId);
                SaveOneTimePurchases();
            }
        }
        
        public bool HasOneTimePurchase(ProductID productId)
        {
            return _oneTimePurchases.Contains(productId);
        }
        
        public void ResetOneTimePurchases()
        {
            _oneTimePurchases.Clear();
            SaveOneTimePurchases();
        }
        
        #endregion
        
        #region Time-Limited Purchases
        
        private void LoadTimeLimitedPurchases()
        {
            var savedData = _saveAdapter.LoadTimeLimitedPurchases();
            if (!string.IsNullOrEmpty(savedData))
            {
                var items = savedData.Split(',');
                foreach (var item in items)
                {
                    if (Enum.TryParse<ProductID>(item, out var productID) && productID != ProductID.NONE)
                    {
                        _timeLimitedPurchases.Add(productID);
                    }
                }
            }
        }
        
        public void SaveTimeLimitedPurchases()
        {
            string saveData = string.Join(",", _timeLimitedPurchases);
            _saveAdapter.SaveTimeLimitedPurchases(saveData);
        }
        
        public void AddTimeLimitedPurchase(ProductID productId)
        {
            if (!_timeLimitedPurchases.Contains(productId))
            {
                _timeLimitedPurchases.Add(productId);
                SaveTimeLimitedPurchases();
            }
        }
        
        public bool HasTimeLimitedPurchase(ProductID productId)
        {
            return _timeLimitedPurchases.Contains(productId);
        }
        
        public void ResetTimeLimitedPurchase(ProductID productId)
        {
            if (_timeLimitedPurchases.Contains(productId))
            {
                _timeLimitedPurchases.Remove(productId);
                SaveTimeLimitedPurchases();
            }
        }
        
        public void ResetAllTimeLimitedPurchases()
        {
            _timeLimitedPurchases.Clear();
            SaveTimeLimitedPurchases();
        }
        
        #endregion
       
        public bool IsPurchaseActive(ProductID productId, ShopItemData itemData)
        {
            if (itemData == null) return false;
            if (itemData.isTimeLimited) return HasTimeLimitedPurchase(productId);
            if (itemData.isOneTimePurchase) return HasOneTimePurchase(productId);
            return false;
        }
  
        public void ResetAllPurchases()
        {
            ResetOneTimePurchases();
            ResetAllTimeLimitedPurchases();
        }
    }
}