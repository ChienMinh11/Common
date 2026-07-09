using System;

namespace ChieChie.Profile
{
    [Serializable]
    public class AvatarModel
    {
        public int Id;
        public string Name;
        public string SpritePath;
        public bool IsUnlocked;
        public string UnlockCondition;
    
        public AvatarModel(int id, string name, string spritePath, bool isUnlocked = false, string unlockCondition = "")
        {
            Id = id;
            Name = name;
            SpritePath = spritePath;
            IsUnlocked = isUnlocked;
            UnlockCondition = unlockCondition;
        }
    }

}
