using ChieChie.Core;
using ChieChie.GamePass;
using UnityEngine;

namespace Game.DependencyInjection
{
    public class GamePassSaveAdapter : IPassSaveAdapter
    {
        private readonly ISaveSystem saveSystem;

        private const string GamePassSaveKey = "GamePass_SaveData_Key";
        private PassSaveData currentRuntimeData;
        public GamePassSaveAdapter(ISaveSystem saveSystem)
        {
            this.saveSystem = saveSystem;
            RegKey();
        }

        void RegKey()
        {
            saveSystem.RegisterKey<PassSaveData>(
                GamePassSaveKey, 
                () => currentRuntimeData, 
                isAutoSave: false
            );
        }
        
        public PassSaveData LoadData()
        {
            currentRuntimeData = saveSystem.Load<PassSaveData>(GamePassSaveKey, defaultValue: null);

            if (currentRuntimeData == null)
            {
                currentRuntimeData = new PassSaveData(); 
            }

            return currentRuntimeData;
        }

        public void SaveData(PassSaveData data)
        {
            if (data == null) return;
            currentRuntimeData = data;
            saveSystem.Save<PassSaveData>(GamePassSaveKey, currentRuntimeData);
        }
    }
}