namespace ChieChie.Constracts
{
    public struct ResourceRewardCommand
    {
        public string ResourceType; 
        public long Amount;
        public bool IsInfinite;
        public float DurationInSeconds;
    }
}