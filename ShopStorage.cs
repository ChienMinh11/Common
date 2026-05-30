using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Core
{
    public class ShopStorage
    {
        // Các khóa lưu trữ riêng biệt
        private const string ONE_TIME_PURCHASES_KEY = "Shop_OneTimePurchases";
        private const string TIME_LIMITED_PURCHASES_KEY = "Shop_TimeLimitedPurchases";
        
        private SaveSystem saveSystem;
        private HashSet<ProductID> oneTimePurchases = new HashSet<ProductID>();
        private HashSet<ProductID> timeLimitedPurchases = new HashSet<ProductID>();
        
        public ShopStorage(SaveSystem saveSystem)
        {
            this.saveSystem = saveSystem;
           
            saveSystem.RegisterKey(ONE_TIME_PURCHASES_KEY);
            saveSystem.RegisterKey(TIME_LIMITED_PURCHASES_KEY);
           
            LoadOneTimePurchases();
            LoadTimeLimitedPurchases();
        }
        
        #region One-Time Purchases
        
        private void LoadOneTimePurchases()
        {
            var savedData = saveSystem.Load<string>(ONE_TIME_PURCHASES_KEY, "");
            if (!string.IsNullOrEmpty(savedData))
            {
                var items = savedData.Split(',');
                foreach (var item in items)
                {
                    if (Enum.TryParse<ProductID>(item, out var productID) && productID != ProductID.NONE)
                    {
                        oneTimePurchases.Add(productID);
                    }
                }
            }
        }
        
        public void SaveOneTimePurchases()
        {
            Debug.Log("Lưu các gói mua một lần");
            string saveData = string.Join(",", oneTimePurchases);
            saveSystem.Save<string>(ONE_TIME_PURCHASES_KEY, saveData);
        }
        
        public void AddOneTimePurchase(ProductID productId)
        {
            if (!oneTimePurchases.Contains(productId))
            {
                oneTimePurchases.Add(productId);
                SaveOneTimePurchases();
            }
        }
        
        public bool HasOneTimePurchase(ProductID productId)
        {
            return oneTimePurchases.Contains(productId);
        }
        
        public void ResetOneTimePurchases()
        {
            oneTimePurchases.Clear();
            SaveOneTimePurchases();
            Debug.Log("Đã đặt lại tất cả các gói mua một lần");
        }
        
        #endregion
        
        #region Time-Limited Purchases
        
        private void LoadTimeLimitedPurchases()
        {
            var savedData = saveSystem.Load<string>(TIME_LIMITED_PURCHASES_KEY, "");
            if (!string.IsNullOrEmpty(savedData))
            {
                var items = savedData.Split(',');
                foreach (var item in items)
                {
                    if (Enum.TryParse<ProductID>(item, out var productID) && productID != ProductID.NONE)
                    {
                        timeLimitedPurchases.Add(productID);
                    }
                }
            }
        }
        
        public void SaveTimeLimitedPurchases()
        {
            Debug.Log("Lưu các gói mua có thời hạn");
            string saveData = string.Join(",", timeLimitedPurchases);
            saveSystem.Save<string>(TIME_LIMITED_PURCHASES_KEY, saveData);
        }
        
        public void AddTimeLimitedPurchase(ProductID productId)
        {
            if (!timeLimitedPurchases.Contains(productId))
            {
                timeLimitedPurchases.Add(productId);
                SaveTimeLimitedPurchases();
            }
        }
        
        public bool HasTimeLimitedPurchase(ProductID productId)
        {
            return timeLimitedPurchases.Contains(productId);
        }
        
        public void ResetTimeLimitedPurchase(ProductID productId)
        {
            if (timeLimitedPurchases.Contains(productId))
            {
                timeLimitedPurchases.Remove(productId);
                SaveTimeLimitedPurchases();
                Debug.Log($"Đã đặt lại gói mua có thời hạn: {productId}");
            }
        }
        
        public void ResetAllTimeLimitedPurchases()
        {
            timeLimitedPurchases.Clear();
            SaveTimeLimitedPurchases();
            Debug.Log("Đã đặt lại tất cả các gói mua có thời hạn");
        }
        
        #endregion
        
        // Kiểm tra trạng thái mua của một sản phẩm (bất kỳ loại nào)
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
        
        // Đặt lại tất cả dữ liệu mua
        public void ResetAllPurchases()
        {
            ResetOneTimePurchases();
            ResetAllTimeLimitedPurchases();
            Debug.Log("Đã đặt lại tất cả dữ liệu mua");
        }
    }
}