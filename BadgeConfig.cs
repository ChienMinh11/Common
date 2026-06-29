using System.Collections.Generic;
using UnityEngine;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "BadgeConfig", menuName = "CORE/Profile/Badge/BadgeConfig")]
    public class BadgeConfig : ScriptableObject
    {
        [SerializeField] private List<BadgeData> badges = new List<BadgeData>();

        public List<BadgeData> Badges => badges;

        public BadgeData GetBadgeById(int id)
        {
            return badges.Find(badge => badge.Id == id);
        }

        public Dictionary<int, BadgeModel> GetDefaultBadgeInfoDictionary()
        {
            var result = new Dictionary<int, BadgeModel>();
            foreach (var badge in badges)
            {
                BadgeModel badgeModel = badge.ToBadgeInfo();
                result[badgeModel.Id] = badgeModel;
            }
            return result;
        }
    }
}