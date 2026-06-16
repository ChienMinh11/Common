using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChieChie.Editor
{
    // ==========================================
    // 1. ĐỊNH NGHĨA SCRIPTABLEOBJECT CẤU HÌNH
    // ==========================================
    public class ProtectedFolderConfig : ScriptableObject
    {
        public bool isProtectionEnabled = true;
        public List<DefaultAsset> protectedFolders = new List<DefaultAsset>();

        private const string ConfigPath = "Assets/Editor/ProtectedFolderConfig.asset";

        public static ProtectedFolderConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ProtectedFolderConfig>(ConfigPath);
            if (config == null)
            {
                config = CreateInstance<ProtectedFolderConfig>();
                
                string directory = System.IO.Path.GetDirectoryName(ConfigPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }
            return config;
        }
    }

    // ==========================================
    // 2. BỘ PHẦN THEO DÕI VÀ NGĂN CHẶN DI CHUYỂN (ĐÃ CẬP NHẬT)
    // ==========================================
    public class ProtectedFolderWatcher : AssetModificationProcessor
    {
        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            var config = ProtectedFolderConfig.GetOrCreateConfig();
            if (config == null || !config.isProtectionEnabled || config.protectedFolders == null)
                return AssetMoveResult.DidNotMove;

            foreach (var folder in config.protectedFolders)
            {
                if (folder == null) continue;

                string protectedFolderPath = AssetDatabase.GetAssetPath(folder);
                if (string.IsNullOrEmpty(protectedFolderPath)) continue;

                bool isSourceInFolder = IsInFolder(sourcePath, protectedFolderPath);
                bool isDestInFolder = IsInFolder(destinationPath, protectedFolderPath);

                // TRƯỜNG HỢP 1: Asset gốc nằm TRONG thư mục bảo vệ
                if (isSourceInFolder)
                {
                    // Nếu đích đến nằm NGOÀI thư mục bảo vệ này
                    if (!isDestInFolder)
                    {
                        Debug.LogError($"[Protection] Không thể di chuyển '{sourcePath}' ra khỏi thư mục được bảo vệ '{protectedFolderPath}'!");
                        return AssetMoveResult.FailedMove;
                    }
            
                    // Không cho phép đổi chỗ nội bộ / đổi tên trong thư mục bảo vệ
                    if (sourcePath != destinationPath)
                    {
                        Debug.LogError($"[Protection] Thư mục '{protectedFolderPath}' đã bị khóa! Không thể đổi vị trí hoặc đổi tên các asset bên trong.");
                        return AssetMoveResult.FailedMove;
                    }
                }
                // TRƯỜNG HỢP 2: Asset gốc nằm NGOÀI nhưng đích đến lại nằm TRONG thư mục bảo vệ
                else if (isDestInFolder)
                {
                    Debug.LogError($"[Protection] Thư mục '{protectedFolderPath}' đã bị khóa! Không thể kéo file từ bên ngoài vào đây.");
                    return AssetMoveResult.FailedMove;
                }
            }
       
            return AssetMoveResult.DidNotMove;
        }

        private static bool IsInFolder(string assetPath, string folderPath)
        {
            if (assetPath == folderPath) return true;
            return assetPath.StartsWith(folderPath + "/");
        }
    }

    // ==========================================
    // 3. GIAO DIỆN CẤU HÌNH TRONG UNITY EDITOR
    // ==========================================
    public class ProtectedFolderSettingsWindow : EditorWindow
    {
        private SerializedObject serializedConfig;
        private SerializedProperty isEnabledProp;
        private SerializedProperty foldersProp;

        [MenuItem("CORE/Protected Folder Settings")]
        public static void ShowWindow()
        {
            GetWindow<ProtectedFolderSettingsWindow>("Folder Protection");
        }

        void OnEnable()
        {
            var config = ProtectedFolderConfig.GetOrCreateConfig();
            if (config != null)
            {
                serializedConfig = new SerializedObject(config);
                isEnabledProp = serializedConfig.FindProperty("isProtectionEnabled");
                foldersProp = serializedConfig.FindProperty("protectedFolders");
            }
        }

        void OnGUI()
        {
            if (serializedConfig == null) return;

            serializedConfig.Update();

            GUILayout.Label("Protected Folder Settings (Team Shared)", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.PropertyField(isEnabledProp, new GUIContent("Enable Protection"));
            GUILayout.Space(10);
            
            EditorGUILayout.PropertyField(foldersProp, new GUIContent("Protected Folders List"), true);
            
            serializedConfig.ApplyModifiedProperties();

            GUILayout.Space(15);
            bool isEnabled = isEnabledProp.boolValue;
            string statusText = $"Status: {(isEnabled ? "ENABLED" : "DISABLED")}\nConfig File: Assets/Editor/ProtectedFolderConfig.asset";
            EditorGUILayout.HelpBox(statusText, MessageType.Info);
        }
    }
}