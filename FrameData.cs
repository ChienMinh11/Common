using UnityEngine;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "FrameData", menuName = "CORE/Profile/Frame/FrameData")]
    public class FrameData : ScriptableObject
    {
        [Header("Frame Information")]
        [SerializeField] private int id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite frameIcon; // Icon tĩnh dùng cho Grid danh sách
        [SerializeField] private GameObject framePrefab; // Prefab chứa Effect/Spine hiển thị chính
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private string unlockCondition;

        public int Id => id;
        public string DisplayName => displayName;
        public Sprite FrameIcon => frameIcon;
        public GameObject FramePrefab => framePrefab; // Property mới
        public bool UnlockedByDefault => unlockedByDefault;
        public string UnlockCondition => unlockCondition;

        public FrameModel ToFrameInfo()
        {
            return new FrameModel(id, displayName, unlockedByDefault);
        }
    }
}