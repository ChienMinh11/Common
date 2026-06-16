using System.Collections;
using UnityEngine;

namespace ChieChie.Core
{
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField]
        private bool showFPS = true;
    
        private float count;
        private int fontSize = 30;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;  // Tắt VSync
            Application.targetFrameRate = 60; // Đặt FPS mục tiêu thành 60
            GUI.depth = 2;
            while (true)
            {
                count = 1f / Time.unscaledDeltaTime;
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnGUI()
        {
            if (showFPS)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = fontSize;
                style.normal.textColor = Color.red;
                GUI.Label(new Rect(5, 40, 300, 50), "FPS: " + Mathf.Round(count), style);
            }
        }
    }
}
