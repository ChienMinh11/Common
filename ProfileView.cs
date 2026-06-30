using System;
using System.Collections.Generic;
using ChieChie.Constracts;
using ChieChie.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class ProfileView : MonoBehaviour, IProfileView
    {
        [Header("Profile Display")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image avatarImage;
        
        // THAY ĐỔI: Sử dụng Transform làm Container chứa thay vì Image tĩnh
        [SerializeField] private Transform frameContainer;  
        [SerializeField] private Transform badgeContainer;  
        [SerializeField] private Button editNameButton;

        [Header("Navigation Controller")]
        [SerializeField] private ProfileTabNavigation profileTabNavigation; 

        [Header("Grid Prefabs & Containers")]
        [SerializeField] private Transform avatarGridContainer;
        [SerializeField] private AvatarView avatarItemPrefab;
        [SerializeField] private Transform frameGridContainer;
        [SerializeField] private FrameItemView frameItemPrefab;
        [SerializeField] private Transform badgeGridContainer;
        [SerializeField] private BadgeItemView badgeItemPrefab;

        public event Action OnEditNameRequested;
        public event Action<string> OnTemporaryNameChanged;
        public event Action<int> OnAvatarSelected;
        public event Action<int> OnFrameSelected; 
        public event Action<int> OnBadgeSelected; 
        public event Action OnSaveRequested;
        public event Action OnCloseRequested;

        private List<AvatarView> _avatarItems = new List<AvatarView>();
        private List<FrameItemView> _frameItems = new List<FrameItemView>(); 
        private List<BadgeItemView> _badgeItems = new List<BadgeItemView>(); 
        
        private int _currentAvatarId;
        private int _currentFrameId; 
        private int _currentBadgeId; 
        private IPopupService _popupController;
        private string _cachedPlayerName;

        // --- HỆ THỐNG POOL NỘI BỘ (MỚI) ---
        // Lưu trữ các Object đã được Instantiate theo ID để tránh spawn/destroy liên tục
        private Dictionary<int, GameObject> _framePool = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> _badgePool = new Dictionary<int, GameObject>();

        // Theo dõi Object nào đang active hiển thị chính trên giao diện
        private GameObject _activeFrameInstance;
        private GameObject _activeBadgeInstance;

        [Inject]
        public void Construct(IPopupService popupService) => _popupController = popupService;

        private void Start()
        {
            if (editNameButton != null)
            {
                editNameButton.onClick.RemoveAllListeners();
                editNameButton.onClick.AddListener(() => OnEditNameRequested?.Invoke());
            }

            if (profileTabNavigation != null)
            {
                profileTabNavigation.OnTabChanged += HandleProfileTabChanged;
            }
        }

        private void OnDestroy()
        {
            if (profileTabNavigation != null)
            {
                profileTabNavigation.OnTabChanged -= HandleProfileTabChanged;
            }

            // Dọn dẹp bộ nhớ Pool khi View bị hủy hoàn toàn
            ClearPools();
        }

        private void HandleProfileTabChanged(int tabIndex)
        {
        }

        // Cập nhật hàm nhận Prefab thay vì Sprite
        public void ShowProfileData(string playerName, Sprite avatarSprite, GameObject framePrefab, GameObject badgePrefab)
        {
            _cachedPlayerName = playerName; 
            UpdatePlayerNameDisplay(playerName);
            if (avatarImage != null && avatarSprite != null) avatarImage.sprite = avatarSprite; 
            
            // Render dữ liệu ban đầu thông qua hàm Update có áp dụng Pool
            UpdateFrameDisplay(_currentFrameId, framePrefab);
            UpdateBadgeDisplay(_currentBadgeId, badgePrefab);
        }

        public void UpdatePlayerNameDisplay(string name)
        {
            if (playerNameText != null) playerNameText.text = name;
            _cachedPlayerName = name;
        }

        public void UpdateAvatarDisplay(int avatarId, Sprite avatarSprite)
        {
            _currentAvatarId = avatarId;
            if (avatarImage != null && avatarSprite != null) avatarImage.sprite = avatarSprite;
            foreach (var item in _avatarItems)
            {
                if (item != null) item.SetSelected(item.GetAvatarId() == _currentAvatarId);
            }
        }

        // THAY ĐỔI: Logic hiển thị Frame sử dụng Object Pool
        public void UpdateFrameDisplay(int frameId, GameObject framePrefab) 
        {
            _currentFrameId = frameId;

            // 1. Ẩn Object đang Active hiện tại (chỉ ẩn, không hủy)
            if (_activeFrameInstance != null)
            {
                _activeFrameInstance.SetActive(false);
                _activeFrameInstance = null;
            }

            if (framePrefab != null && frameContainer != null)
            {
                // 2. Kiểm tra xem item này đã từng được sinh trong Pool chưa
                if (!_framePool.TryGetValue(frameId, out var pooledInstance) || pooledInstance == null)
                {
                    // Nếu chưa có, tiến hành Instantiate mới và nạp vào Pool
                    pooledInstance = Instantiate(framePrefab, frameContainer);
                    pooledInstance.transform.localPosition = Vector3.zero;
                    pooledInstance.transform.localRotation = Quaternion.identity;
                    pooledInstance.transform.localScale = Vector3.one;
                    _framePool[frameId] = pooledInstance;
                }

                // 3. Kích hoạt lại Object từ Pool và đặt làm ActiveInstance
                pooledInstance.SetActive(true);
                _activeFrameInstance = pooledInstance;
            }

            // Cập nhật trạng thái ô Grid viền chọn
            foreach (var item in _frameItems)
            {
                if (item != null) item.SetSelected(item.GetFrameId() == _currentFrameId);
            }
        }

        // THAY ĐỔI: Logic hiển thị Badge sử dụng Object Pool
        public void UpdateBadgeDisplay(int badgeId, GameObject badgePrefab) 
        {
            _currentBadgeId = badgeId;

            // 1. Ẩn Object đang Active hiện tại
            if (_activeBadgeInstance != null)
            {
                _activeBadgeInstance.SetActive(false);
                _activeBadgeInstance = null;
            }

            if (badgePrefab != null && badgeContainer != null)
            {
                // 2. Kiểm tra xem item này đã từng được sinh trong Pool chưa
                if (!_badgePool.TryGetValue(badgeId, out var pooledInstance) || pooledInstance == null)
                {
                    // Nếu chưa có, tiến hành Instantiate mới và nạp vào Pool
                    pooledInstance = Instantiate(badgePrefab, badgeContainer);
                    pooledInstance.transform.localPosition = Vector3.zero;
                    pooledInstance.transform.localRotation = Quaternion.identity;
                    pooledInstance.transform.localScale = Vector3.one;
                    _badgePool[badgeId] = pooledInstance;
                }

                // 3. Kích hoạt lại Object và thiết lập hiển thị container
                pooledInstance.SetActive(true);
                _activeBadgeInstance = pooledInstance;
                badgeContainer.gameObject.SetActive(true);
            }
            else if (badgePrefab == null && badgeContainer != null)
            {
                // Trường hợp tháo gỡ Badge (badgeId = -1), ẩn container đi
                badgeContainer.gameObject.SetActive(false);
            }
    
            foreach (var item in _badgeItems)
            {
                if (item != null) item.SetSelected(item.GetBadgeId() == _currentBadgeId);
            }
        }

        public void PopulateAvatarGrid(List<AvatarDisplayData> avatars)
        {
            ClearGrid(_avatarItems);
            foreach (var avatar in avatars)
            {
                if (avatarItemPrefab != null && avatarGridContainer != null)
                {
                    AvatarView newItem = Instantiate(avatarItemPrefab, avatarGridContainer);
                    newItem.Initialize(avatar.Id, avatar.Name, avatar.Sprite, avatar.IsUnlocked, avatar.Id == _currentAvatarId, OnAvatarGridItemSelected);
                    _avatarItems.Add(newItem);
                }
            }
        }

        public void PopulateFrameGrid(List<FrameDisplayData> frames) 
        {
            ClearGrid(_frameItems);
            foreach (var frame in frames)
            {
                if (frameItemPrefab != null && frameGridContainer != null)
                {
                    FrameItemView newItem = Instantiate(frameItemPrefab, frameGridContainer);
                    // Đã truyền đầy đủ cả frame.Icon (ảnh tĩnh) và frame.Prefab (hiệu ứng động cho Pool nội bộ ô grid)
                    newItem.Initialize(frame.Id, frame.Name, frame.Icon, frame.Prefab, frame.IsUnlocked, frame.Id == _currentFrameId, OnFrameGridItemSelected);
                    _frameItems.Add(newItem);
                }
            }
        }

        public void PopulateBadgeGrid(List<BadgeDisplayData> badges) 
        {
            ClearGrid(_badgeItems);
            foreach (var badge in badges)
            {
                if (badgeItemPrefab != null && badgeGridContainer != null)
                {
                    BadgeItemView newItem = Instantiate(badgeItemPrefab, badgeGridContainer);
                    // Đã truyền đầy đủ cả badge.Icon và badge.Prefab
                    newItem.Initialize(badge.Id, badge.Name, badge.Icon, badge.Prefab, badge.IsUnlocked, badge.Id == _currentBadgeId, OnBadgeGridItemSelected);
                    _badgeItems.Add(newItem);
                }
            }
        }

        private void ClearGrid<T>(List<T> list) where T : MonoBehaviour
        {
            foreach (var item in list) if (item != null) Destroy(item.gameObject);
            list.Clear();
        }

        // Hàm xóa và giải phóng toàn bộ các Object nằm trong Pool khi đóng/hủy View
        private void ClearPools()
        {
            foreach (var kvp in _framePool)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _framePool.Clear();

            foreach (var kvp in _badgePool)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _badgePool.Clear();

            _activeFrameInstance = null;
            _activeBadgeInstance = null;
        }

        public void SetSaveButtonInteractable(bool interactable) { }

        private void OnAvatarGridItemSelected(int avatarId) => OnAvatarSelected?.Invoke(avatarId);
        private void OnFrameGridItemSelected(int frameId) => OnFrameSelected?.Invoke(frameId); 
        private void OnBadgeGridItemSelected(int badgeId) => OnBadgeSelected?.Invoke(badgeId); 
    }
}