// FrameModel.cs
using System;

namespace ChieChie.Profile
{
    [Serializable]
    public class FrameModel
    {
        public int Id;
        public string Name;
        public bool IsUnlocked;

        public FrameModel(int id, string name, bool isUnlocked = false)
        {
            Id = id;
            Name = name;
            IsUnlocked = isUnlocked;
        }
    }
}