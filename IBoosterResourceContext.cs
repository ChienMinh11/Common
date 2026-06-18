namespace ChieChie.Booster
{
    public interface IBoosterResourceContext
    { 
        bool IsCurrentlyInfinite(string type);
        bool HasEnoughResource(string type, long cost);
        void SpendResource(string type, long cost);
        void AddResource(string type, long cost);
    }
}
