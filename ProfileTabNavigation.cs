using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay
{
    [Serializable]
    public struct ProfileTabGroup
    {
        public Button TabButton;
        public GameObject TabPanel;
    }

    public class ProfileTabNavigation : MonoBehaviour
    {
        [SerializeField] private List<ProfileTabGroup> tabs = new List<ProfileTabGroup>();
        [SerializeField] private int defaultTabIndex = 0;

        public event Action<int> OnTabChanged;
        private int _currentTabIndex = -1;

        private void Start()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i; // Tránh việc closure chiếm dụng biến i chạy trong vòng lặp
                if (tabs[index].TabButton != null)
                {
                    tabs[index].TabButton.onClick.RemoveAllListeners();
                    tabs[index].TabButton.onClick.AddListener(() => SwitchTab(index));
                }
            }

            // Hiển thị tab mặc định ban đầu
            SwitchTab(defaultTabIndex);
        }

        public void SwitchTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabs.Count || _currentTabIndex == tabIndex) return;

            _currentTabIndex = tabIndex;

            for (int i = 0; i < tabs.Count; i++)
            {
                bool isActive = (i == tabIndex);
                
                // Bật/Tắt Panel nội dung
                if (tabs[i].TabPanel != null)
                {
                    tabs[i].TabPanel.SetActive(isActive);
                }

                // Cập nhật hiệu ứng Visual cho Button (Highlight tab đang chọn)
                if (tabs[i].TabButton != null)
                {
                    UpdateTabButtonVisual(tabs[i].TabButton, isActive);
                }
            }

            OnTabChanged?.Invoke(tabIndex);
        }

        private void UpdateTabButtonVisual(Button btn, bool isActive)
        {
            float alpha = isActive ? 1f : 0.5f;
            
            if (btn.TryGetComponent<CanvasGroup>(out var group))
            {
                group.alpha = alpha;
            }
            else if (btn.image != null)
            {
                Color c = btn.image.color;
                c.a = alpha;
                btn.image.color = c;
            }
        }
    }
}