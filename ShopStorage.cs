using System;
using System.Collections.Generic;
using ChieChie.Core;
using UnityEngine;

namespace ChieChie.Shop
{
    public class ShopStorage
    {
        private const string ONE_TIME_PURCHASES_KEY = "Shop_OneTimePurchases";
        private const string TIME_LIMITED_PURCHASES_KEY = "Shop_TimeLimitedPurchases";
        
        private readonly ISaveSystem _saveSystem;
        private readonly HashSet<ProductID> _oneTimePurchases = new HashSet<ProductID>();
        private readonly HashSet<ProductID> _timeLimitedPurchases = new HashSet<ProductID>();
        
        public ShopStorage(ISaveSystem saveSystem)
        {
            this._saveSystem = saveSystem;
           
            saveSystem.RegisterKey(ONE_TIME_PURCHASES_KEY);
            saveSystem.RegisterKey(TIME_LIMITED_PURCHASES_KEY);
           
            LoadOneTimePurchases();
            LoadTimeLimitedPurchases();
        }
        
        #region One-Time Purchases
        
        private void LoadOneTimePurchases()
        {
            var savedData = _saveSystem.Load<string>(ONE_TIME_PURCHASES_KEY, "");
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
            Debug.Log("Lưu các gói mua một lần");
            string saveData = string.Join(",", _oneTimePurchases);
            _saveSystem.Save<string>(ONE_TIME_PURCHASES_KEY, saveData);
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
            Debug.Log("Đã đặt lại tất cả các gói mua một lần");
        }
        
        #endregion
        
        #region Time-Limited Purchases
        
        private void LoadTimeLimitedPurchases()
        {
            var savedData = _saveSystem.Load<string>(TIME_LIMITED_PURCHASES_KEY, "");
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
            Debug.Log("Lưu các gói mua có thời hạn");
            string saveData = string.Join(",", _timeLimitedPurchases);
            _saveSystem.Save<string>(TIME_LIMITED_PURCHASES_KEY, saveData);
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
                Debug.Log($"Đã đặt lại gói mua có thời hạn: {productId}");
            }
        }
        
        public void ResetAllTimeLimitedPurchases()
        {
            _timeLimitedPurchases.Clear();
            SaveTimeLimitedPurchases();
            Debug.Log("Đã đặt lại tất cả các gói mua có thời hạn");
        }
        
        #endregion
       
        public bool IsPurchaseActive(ProductID productId, ShopItemData itemData)
        {
            if (itemData == null)
                return false;
                
            if (itemData.isTimeLimited)
            {
                return HasTimeLimitedPurchase(productId);
            }
            else if (itemData.isOneTimePurchase)
            {
                return HasOneTimePurchase(productId);
            }
            
            return false;
        }
  
        public void ResetAllPurchases()
        {
            ResetOneTimePurchases();
            ResetAllTimeLimitedPurchases();
            Debug.Log("Đã đặt lại tất cả dữ liệu mua");
        }
    }
}