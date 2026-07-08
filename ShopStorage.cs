using System;
using System.Collections.Generic;
using ChieChie.Constracts;

namespace ChieChie.Shop
{
    public class ShopStorage
    {
        private readonly IShopSaveAdapter _saveAdapter;
        private readonly HashSet<string> _oneTimePurchases = new HashSet<string>();
        private readonly HashSet<string> _timeLimitedPurchases = new HashSet<string>();
        
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
                    if (!string.IsNullOrEmpty(item))
                    {
                        _oneTimePurchases.Add(item.Trim());
                    }
                }
            }
        }
        
        public void SaveOneTimePurchases()
        {
            string saveData = string.Join(",", _oneTimePurchases);
            _saveAdapter.SaveOneTimePurchases(saveData);
        }
        
        public void AddOneTimePurchase(string productId)
        {
            if (!_oneTimePurchases.Contains(productId))
            {
                _oneTimePurchases.Add(productId);
                SaveOneTimePurchases();
            }
        }
        
        public bool HasOneTimePurchase(string productId)
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
                    if (!string.IsNullOrEmpty(item))
                    {
                        _timeLimitedPurchases.Add(item.Trim());
                    }
                }
            }
        }
        
        public void SaveTimeLimitedPurchases()
        {
            string saveData = string.Join(",", _timeLimitedPurchases);
            _saveAdapter.SaveTimeLimitedPurchases(saveData);
        }
        
        public void AddTimeLimitedPurchase(string productId)
        {
            if (!_timeLimitedPurchases.Contains(productId))
            {
                _timeLimitedPurchases.Add(productId);
                SaveTimeLimitedPurchases();
            }
        }
        
        public bool HasTimeLimitedPurchase(string productId)
        {
            return _timeLimitedPurchases.Contains(productId);
        }
        
        public void ResetTimeLimitedPurchase(string productId)
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
       
        public bool IsPurchaseActive(string productId, IShopItemData itemData)
        {
            if (itemData == null) return false;
            if (itemData.IsTimeLimited) return HasTimeLimitedPurchase(productId);
            if (itemData.IsOneTimePurchase) return HasOneTimePurchase(productId);
            return false;
        }
    }
}