using ChieChie.Core;
using ChieChie.Profile;

namespace Game.DependencyInjection
{
    public class ProfileSaveAdapter : IProfileSaveAdapter
    {
        private const string PROFILE_SAVE_KEY = "Player_Profile_SaveData_Key";

        private readonly ISaveSystem _saveSystem;
        private ProfileSaveData _currentRuntimeData;

        public ProfileSaveAdapter(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
            
            // Đăng ký 1 Key duy nhất cho toàn bộ Profile của người chơi
            _saveSystem.RegisterKey<ProfileSaveData>(
                PROFILE_SAVE_KEY, 
                () => _currentRuntimeData, 
                isAutoSave: false
            );
        }

        public ProfileSaveData LoadData()
        {
            _currentRuntimeData = _saveSystem.Load<ProfileSaveData>(PROFILE_SAVE_KEY, null);

            if (_currentRuntimeData == null)
            {
                _currentRuntimeData = new ProfileSaveData();
            }

            return _currentRuntimeData;
        }

        public void SaveData(ProfileSaveData data)
        {
            if (data == null) return;
            _currentRuntimeData = data;
            _saveSystem.Save<ProfileSaveData>(PROFILE_SAVE_KEY, _currentRuntimeData);
        }
    }
}