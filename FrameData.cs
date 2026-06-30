using UnityEngine;

namespace ChieChie.Profile
{
    [CreateAssetMenu(fileName = "FrameData", menuName = "CORE/Profile/Frame/FrameData")]
    public class FrameData : ScriptableObject
    {
        [Header("Frame Information")]
        [SerializeField] private int id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite frameIcon; 
        [SerializeField] private GameObject framePrefab; 
        [SerializeField] private bool lockByDefault;
        

        public int Id => id;
        public string DisplayName => displayName;
        public Sprite FrameIcon => frameIcon;
        public GameObject FramePrefab => framePrefab; 
        public bool LockByDefault => lockByDefault;
      

        public FrameModel ToFrameInfo()
        {
            bool initialUnlocked = lockByDefault;
            return new FrameModel(id, displayName, initialUnlocked);
        }
    }
}