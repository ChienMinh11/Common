using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ChieChie.Editor
{
    public static class GameViewAspectRatioHelper
    {
        public static void SetAspectRatio(string aspectRatio)
        {
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                {
                    Debug.LogWarning("Không tìm thấy GameView type");
                    return;
                }

                var gameView = EditorWindow.GetWindow(gameViewType);
                if (gameView == null)
                {
                    Debug.LogWarning("Không tìm thấy GameView window");
                    return;
                }

                // CODE MỚI BẮT ĐẦU TỪ ĐÂY
                var selectedSizeIndexProp = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (selectedSizeIndexProp == null)
                {
                    Debug.LogWarning("Không tìm thấy selectedSizeIndex property");
                    return;
                }

                int targetIndex = aspectRatio switch
                {
                    "Free Aspect" => 0,
                    "9:16" => 5,
                    "16:9" => 6,
                    _ => 0
                };

                selectedSizeIndexProp.SetValue(gameView, targetIndex);
                gameView.Focus();
                gameView.Repaint();
                Debug.Log($"✓ Đã set aspect ratio: {aspectRatio}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi set aspect ratio: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}

