using System;

namespace ChieChie.GenericLiveOps
{
    public interface IEventSaveAdapter<TSaveData> where TSaveData : class
    {
        TSaveData LoadData();
        void SaveData(TSaveData data);
    }

    public class EventSaveController<TSaveData> where TSaveData : class, new()
    {
        private readonly IEventSaveAdapter<TSaveData> _saveAdapter;
        private readonly Action<TSaveData> _ensureDefaults;

        public TSaveData Data { get; private set; } = new TSaveData();

        public EventSaveController(IEventSaveAdapter<TSaveData> saveAdapter)
            : this(saveAdapter, _ => { })
        {
        }

        public EventSaveController(IEventSaveAdapter<TSaveData> saveAdapter, Action<TSaveData> ensureDefaults)
        {
            _saveAdapter = saveAdapter ?? throw new ArgumentNullException(nameof(saveAdapter));
            _ensureDefaults = ensureDefaults ?? (_ => { });
        }

        public TSaveData LoadOrCreate()
        {
            Data = _saveAdapter.LoadData() ?? new TSaveData();
            EnsureDefaults(Data);
            return Data;
        }

        public TSaveData Replace(TSaveData data, bool saveImmediately = true)
        {
            Data = data ?? new TSaveData();
            EnsureDefaults(Data);

            if (saveImmediately)
            {
                _saveAdapter.SaveData(Data);
            }

            return Data;
        }

        public void Save()
        {
            if (Data == null)
            {
                Data = new TSaveData();
            }

            EnsureDefaults(Data);
            _saveAdapter.SaveData(Data);
        }

        public void Save(TSaveData data)
        {
            Data = data ?? new TSaveData();
            Save();
        }

        private void EnsureDefaults(TSaveData data)
        {
            _ensureDefaults(data);
        }
    }
}
