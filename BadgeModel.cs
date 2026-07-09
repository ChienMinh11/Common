// BadgeModel.cs
using System;

namespace ChieChie.Profile
{
    [Serializable]
    public class BadgeModel
    {
        public int Id;
        public string Name;
        public bool IsUnlocked;

        public BadgeModel(int id, string name, bool isUnlocked = false)
        {
            Id = id;
            Name = name;
            IsUnlocked = isUnlocked;
        }
    }
}