using System;

namespace ChieChie.Profile
{
    [Serializable]
    public class ProfileModel
    {
        public string PlayerName;
        public int AvatarId;
        public DateTime CreationDate;
        public DateTime LastModified;
        
        public ProfileModel()
        {
            PlayerName = "You";
            AvatarId = 0; // Default avatar ID (0 is typically first/default)
            CreationDate = DateTime.Now;
            LastModified = DateTime.Now;
        }
        
        public ProfileModel Clone()
        {
            return new ProfileModel
            {
                PlayerName = this.PlayerName,
                AvatarId = this.AvatarId,
                CreationDate = this.CreationDate,
                LastModified = DateTime.Now
            };
        }
        
        public void UpdateLastModified()
        {
            LastModified = DateTime.Now;
        }
    }
}