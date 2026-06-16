using UnityEngine;

namespace ChieChie.Core
{
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect safeArea;
        private ScreenOrientation lastOrientation;
        private Vector2 lastScreenSize;

        private void Awake()
        {
            // Đảm bảo có RectTransform
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("SafeAreaHandler: Missing RectTransform component!");
                return;
            }

            Init();
        }

        private void Init()
        {
            lastOrientation = Screen.orientation;
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            RefreshSafeAreaSize();
        }

        private void OnEnable()
        {
            // Kiểm tra null trước khi subscribe
            if (rectTransform != null)
            {
                Application.focusChanged += OnApplicationFocusChanged;
            }
        }

        private void OnDisable()
        {
            Application.focusChanged -= OnApplicationFocusChanged;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (rectTransform == null) return;

            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
            ScreenOrientation currentOrientation = Screen.orientation;

            if (lastOrientation != currentOrientation || lastScreenSize != currentScreenSize)
            {
                lastOrientation = currentOrientation;
                lastScreenSize = currentScreenSize;
                RefreshSafeAreaSize();
            }
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (rectTransform == null) return;

            if (hasFocus)
            {
                RefreshSafeAreaSize();
            }
        }

        private void RefreshSafeAreaSize()
        {
            if (rectTransform == null) return;

            Rect newSafeArea = Screen.safeArea;

            if (safeArea != newSafeArea)
            {
                safeArea = newSafeArea;
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (rectTransform == null) return;

            // Tính toán các anchor points dựa trên safe area
            Vector2 minAnchor = safeArea.position;
            Vector2 maxAnchor = minAnchor + safeArea.size;

            // Chuyển đổi sang tỉ lệ từ 0-1
            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            // Áp dụng các anchor points mới
            rectTransform.anchorMin = minAnchor;
            rectTransform.anchorMax = maxAnchor;
        }

        public void ForceRefresh()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError("SafeAreaHandler: Missing RectTransform component!");
                    return;
                }
            }

            RefreshSafeAreaSize();
        }
    }
}
