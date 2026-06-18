using UnityEngine;

namespace ChieChie.Booster
{
    public abstract class BoosterSetting : ScriptableObject
    {
        [Header("Identity Reference")]
        [Tooltip("Kéo thả ScriptableObject (có implement IBoosterIdentitySource) vào đây")]
        [SerializeField] private UnityEngine.Object identitySource; 
        public UnityEngine.Object IdentitySource => identitySource;
        public IBoosterIdentitySource Identity => identitySource as IBoosterIdentitySource;
        public string BoosterId {
            get 
            {
                if (Identity != null && !string.IsNullOrEmpty(Identity.BoosterId))
                {
                    return Identity.BoosterId;
                }
                return string.Empty; 
            }
        }

        [Header("Booster Configs")]
        [SerializeField] private string boosterName;
        [SerializeField] private string description;
        [SerializeField] private BoosterType boosterType;
        [SerializeField] private GameObject behaviorPrefab;
        [SerializeField] private int requiredLevel;
        [SerializeField] private string floatingMessage;
        [SerializeField] private long cost = 1;

        // Getters
        public string BoosterName => boosterName;
        public string Description => description;
        public BoosterType BoosterType => boosterType;
        public GameObject BehaviorPrefab => behaviorPrefab;
        public int RequiredLevel => requiredLevel;
        public string FloatingMessage => floatingMessage;
        public long Cost => cost; 

        public abstract void Initialise();
        
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (identitySource != null)
            {
                if (!(identitySource is IBoosterIdentitySource))
                {
                    Debug.LogError(
                        $"<color=red><b>[BoosterSetting LỖI]:</b></color> đang kéo file '{identitySource.name}' " +
                        $"KHÔNG triển khai interface IBoosterIdentitySource! Hệ thống đã tự động gỡ bỏ liên kết.");
                 
                    identitySource = null;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
      
                else if (identitySource is IBoosterIdentitySource identity && string.IsNullOrEmpty(identity.BoosterId))
                {
                    Debug.LogWarning(
                        $"<color=yellow><b>[BoosterSetting CẢNH BÁO]:</b></color> " +
                        $"đang dùng file '{identitySource.name}' nhưng file này đang BỎ TRỐNG trường BoosterId! " +
                        $"Vui lòng nhập ID tĩnh cho asset này để đảm bảo an toàn Save/Load.");
                }
            }
        }
#endif
    }
}