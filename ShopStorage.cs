using System;
using System.Collections.Generic;
using System.Globalization;
using ChieChie.Constracts;

namespace ChieChie.Shop
{
    public class ShopStorage
    {
        private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly IShopSaveAdapter _saveAdapter;
        private readonly HashSet<string> _oneTimePurchases = new HashSet<string>();
        private readonly HashSet<string> _timeLimitedPurchases = new HashSet<string>();
        private readonly Dictionary<string, long> _relativeOfferStartTimes = new Dictionary<string, long>();
        
        public ShopStorage(IShopSaveAdapter saveAdapter)
        {
            this._saveAdapter = saveAdapter;
            LoadOneTimePurchases();
            LoadTimeLimitedPurchases();
            LoadRelativeOfferStartTimes();
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

        #region Relative Offer Start Times

        private void LoadRelativeOfferStartTimes()
        {
            var savedData = _saveAdapter.LoadRelativeOfferStartTimes();
            if (string.IsNullOrEmpty(savedData)) return;

            var entries = savedData.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                int separatorIndex = entry.LastIndexOf(':');
                if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1) continue;

                string productId = Uri.UnescapeDataString(entry.Substring(0, separatorIndex));
                string rawUnixSeconds = entry.Substring(separatorIndex + 1);

                if (string.IsNullOrEmpty(productId)) continue;
                if (long.TryParse(rawUnixSeconds, NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out long unixSeconds))
                {
                    _relativeOfferStartTimes[productId] = unixSeconds;
                }
            }
        }

        private void SaveRelativeOfferStartTimes()
        {
            var entries = new List<string>(_relativeOfferStartTimes.Count);
            foreach (var kvp in _relativeOfferStartTimes)
            {
                string productId = Uri.EscapeDataString(kvp.Key);
                string unixSeconds = kvp.Value.ToString(CultureInfo.InvariantCulture);
                entries.Add($"{productId}:{unixSeconds}");
            }

            _saveAdapter.SaveRelativeOfferStartTimes(string.Join("|", entries));
        }

        public bool TryGetRelativeOfferStartTime(string productId, out DateTime startTimeUtc)
        {
            startTimeUtc = default(DateTime);
            if (string.IsNullOrEmpty(productId)) return false;
            if (!_relativeOfferStartTimes.TryGetValue(productId, out long unixSeconds)) return false;

            try
            {
                startTimeUtc = UnixEpochUtc.AddSeconds(unixSeconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        public void SetRelativeOfferStartTime(string productId, DateTime startTimeUtc)
        {
            if (string.IsNullOrEmpty(productId)) return;
            if (startTimeUtc.Kind != DateTimeKind.Utc) startTimeUtc = startTimeUtc.ToUniversalTime();

            long unixSeconds = (long)Math.Floor((startTimeUtc - UnixEpochUtc).TotalSeconds);
            if (_relativeOfferStartTimes.TryGetValue(productId, out long existingUnixSeconds)
                && existingUnixSeconds == unixSeconds)
            {
                return;
            }

            _relativeOfferStartTimes[productId] = unixSeconds;
            SaveRelativeOfferStartTimes();
        }

        public void ResetRelativeOfferStartTime(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;
            if (_relativeOfferStartTimes.Remove(productId))
            {
                SaveRelativeOfferStartTimes();
            }
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
