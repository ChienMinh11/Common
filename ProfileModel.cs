using System;

namespace ChieChie.Profile
{
    [Serializable]
    public class ProfileModel
    {
        public string PlayerName;
        public int AvatarId;
        public int FrameId;
        public int BadgeId; 
        public DateTime CreationDate;
        public DateTime LastModified;
        
        public ProfileModel()
        {
            PlayerName = "You";
            AvatarId = 0; 
            FrameId = 0;  
            BadgeId = -1;  
            CreationDate = DateTime.Now;
            LastModified = DateTime.Now;
        }
        
        public ProfileModel Clone()
        {
            return new ProfileModel
            {
                PlayerName = this.PlayerName,
                AvatarId = this.AvatarId,
                FrameId = this.FrameId,   
                BadgeId = this.BadgeId,
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