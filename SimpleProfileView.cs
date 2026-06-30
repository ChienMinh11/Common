using System.Collections.Generic;
using ChieChie.Constracts;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.GamePlay
{
    public class SimpleProfileView : MonoBehaviour, ISimpleProfileView
    {
        [Header("UI Components")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private Transform frameContainer;
        [SerializeField] private Transform badgeContainer;

        private IProfileService _presenter;

        private Dictionary<int, GameObject> _framePool = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> _badgePool = new Dictionary<int, GameObject>();
        private GameObject _activeFrameInstance;
        private GameObject _activeBadgeInstance;

        [Inject]
        public void Construct(IProfileService presenter)
        {
            _presenter = presenter;
            _presenter?.RegisterSimpleView(this);
        }
        public void UpdateAvatar(Sprite avatarSprite)
        {
            if (avatarImage != null)
            {
                avatarImage.gameObject.SetActive(avatarSprite != null);
                avatarImage.sprite = avatarSprite;
            }
        }

        public void UpdateFrame(int frameId, GameObject framePrefab)
        {
            if (_activeFrameInstance != null) _activeFrameInstance.SetActive(false);
            if (framePrefab == null) return;

            if (!_framePool.TryGetValue(frameId, out var instance) || instance == null)
            {
                instance = Instantiate(framePrefab, frameContainer);
                _framePool[frameId] = instance;
            }
            instance.SetActive(true);
            _activeFrameInstance = instance;
        }

        public void UpdateBadge(int badgeId, GameObject badgePrefab)
        {
            if (_activeBadgeInstance != null) _activeBadgeInstance.SetActive(false);

            if (badgePrefab == null)
            {
                if (badgeContainer != null) badgeContainer.gameObject.SetActive(false);
                return;
            }

            if (badgeContainer != null) badgeContainer.gameObject.SetActive(true);

            if (!_badgePool.TryGetValue(badgeId, out var instance) || instance == null)
            {
                instance = Instantiate(badgePrefab, badgeContainer);
                _badgePool[badgeId] = instance;
            }
            instance.SetActive(true);
            _activeBadgeInstance = instance;
        }

        private void OnDestroy()
        {
            _presenter.UnregisterSimpleView(this);
            foreach (var kvp in _framePool) if (kvp.Value != null) Destroy(kvp.Value);
            foreach (var kvp in _badgePool) if (kvp.Value != null) Destroy(kvp.Value);
            
        }
    }
}