using System;

namespace ChieChie.Profile
{
    [Serializable]
    public class ProfileModel
    {
        public string PlayerName;
        public int AvatarId;
        public int FrameId;  // Thêm mới
        public int BadgeId;  // Thêm mới
        public DateTime CreationDate;
        public DateTime LastModified;
        
        public ProfileModel()
        {
            PlayerName = "You";
            AvatarId = 0; 
            FrameId = 0;   // Mặc định ban đầu
            BadgeId = 0;   // Mặc định ban đầu
            CreationDate = DateTime.Now;
            LastModified = DateTime.Now;
        }
        
        public ProfileModel Clone()
        {
            return new ProfileModel
            {
                PlayerName = this.PlayerName,
                AvatarId = this.AvatarId,
                FrameId = this.FrameId,   // Thêm mới
                BadgeId = this.BadgeId,   // Thêm mới
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