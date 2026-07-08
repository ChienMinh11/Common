using System;

namespace ChieChie.Shop
{
    public interface IShopTimeProvider
    {
        DateTime UtcNow { get; }
    }

    public class SystemUtcShopTimeProvider : IShopTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
