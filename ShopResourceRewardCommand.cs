using ChieChie.Core;

namespace ChieChie.Shop
{
    public struct ShopResourceRewardCommand
    {
        public ResourceType ResourceType;
        public long Amount;
        public bool IsInfinite;
        public float DurationInSeconds;
    }
}
