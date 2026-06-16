namespace ChieChie.Core
{
    public class EasySaveLoadStrategy : ISaveLoadStrategy
    {
        public void Save<T>(string key, T value)
        {
             ES3.Save(key, value);
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            return ES3.Load(key, defaultValue);
        }

        public void Delete(string key)
        {
            ES3.DeleteKey(key);
        }

        public void DeleteAll()
        {
            ES3.DeleteFile("SaveFile.es3");
            
        }
       
    }
}
