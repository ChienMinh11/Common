using ChieChie.Constracts;
using ChieChie.Core;
using ChieChie.Shop;

namespace Game.GamePlay
{
    public class ShopSaveAdapter : IShopSaveAdapter
    {
        private const string ONE_TIME_PURCHASES_KEY = "Shop_OneTimePurchases";
        private const string TIME_LIMITED_PURCHASES_KEY = "Shop_TimeLimitedPurchases";
        private const string RELATIVE_OFFER_START_TIMES_KEY = "Shop_RelativeOfferStartTimes";

        private readonly ISaveSystem _saveSystem;

        public ShopSaveAdapter(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
            
            // Đăng ký key với hệ thống Save khi khởi tạo adapter bên ngoài
            _saveSystem.RegisterKey(ONE_TIME_PURCHASES_KEY);
            _saveSystem.RegisterKey(TIME_LIMITED_PURCHASES_KEY);
            _saveSystem.RegisterKey(RELATIVE_OFFER_START_TIMES_KEY);
        }

        public string LoadOneTimePurchases()
        {
            return _saveSystem.Load<string>(ONE_TIME_PURCHASES_KEY, "");
        }

        public void SaveOneTimePurchases(string data)
        {
            _saveSystem.Save<string>(ONE_TIME_PURCHASES_KEY, data);
        }

        public string LoadTimeLimitedPurchases()
        {
            return _saveSystem.Load<string>(TIME_LIMITED_PURCHASES_KEY, "");
        }

        public void SaveTimeLimitedPurchases(string data)
        {
            _saveSystem.Save<string>(TIME_LIMITED_PURCHASES_KEY, data);
        }

        public string LoadRelativeOfferStartTimes()
        {
            return _saveSystem.Load<string>(RELATIVE_OFFER_START_TIMES_KEY, "");
        }

        public void SaveRelativeOfferStartTimes(string data)
        {
            _saveSystem.Save<string>(RELATIVE_OFFER_START_TIMES_KEY, data);
        }
    }
}
