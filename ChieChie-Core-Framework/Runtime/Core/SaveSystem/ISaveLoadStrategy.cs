namespace ChieChie.Core
{
    public interface ISaveLoadStrategy
    {
        void Save<T>(string key, T value);
        T Load<T>(string key, T defaultValue = default);
        void Delete(string key);
        void DeleteAll();
        
    }
}
