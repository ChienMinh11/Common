using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChieChie.Editor
{
    [Serializable]
    public class SceneAspectRatioList
    {
        public List<string> scenePaths = new List<string>();
        public List<string> aspectRatios = new List<string>();
    }
    public class TestWindow : OdinEditorWindow
    {
        [Serializable]
        private class SceneAspectRatioData
        {
            public string scenePath;
            public string aspectRatio;
        }
        
        [MenuItem("CORE/Test Window")]
        private static void OpenWindow()
        {
            GetWindow<TestWindow>().Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            AutoSetKeystorePasswordFromEnv();
            LoadSceneAspectRatios();
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        protected override void OnDisable()
        {
            base.OnDisable();
    
            // Unsubscribe để tránh memory leak
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (!AutoApplyAspectsEnabled) return;
            string scenePath = scene.path;
            EditorApplication.delayCall += () =>
            {
                ApplyAspectRatio(scenePath);
            };
        }
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!AutoApplyAspectsEnabled) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                ApplyAspectRatio(activeScene.path);
            }
        }

        #region Keystore Settings

        [FoldoutGroup("Keystore Settings")]
        [InfoBox("Nhập password để lưu vào biến môi trường (chỉ cần làm 1 lần)", InfoMessageType.Info)]
        [LabelText("Keystore Password")]
        [SerializeField] private string keystorePassword;

        [FoldoutGroup("Keystore Settings")]
        [LabelText("Key Alias Password")]
        [SerializeField] private string keyAliasPassword;

        [FoldoutGroup("Keystore Settings")]
        [Button("Setup Environment Variables", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.6f, 1f)]
        private void SetupEnvironmentVariables()
        {
            if (string.IsNullOrEmpty(keystorePassword) || string.IsNullOrEmpty(keyAliasPassword))
            {
                EditorUtility.DisplayDialog("Lỗi", 
                    "Vui lòng nhập đầy đủ Keystore Password và Key Alias Password!", 
                    "OK");
                return;
            }

            try
            {
                // Set biến môi trường cho user hiện tại
                Environment.SetEnvironmentVariable("UNITY_KEYSTORE_PASS", keystorePassword, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("UNITY_KEY_ALIAS_PASS", keyAliasPassword, EnvironmentVariableTarget.User);

                // Set luôn vào PlayerSettings
                PlayerSettings.Android.keystorePass = keystorePassword;
                PlayerSettings.Android.keyaliasPass = keyAliasPassword;

                Debug.Log("✓ Đã setup biến môi trường thành công!");
                EditorUtility.DisplayDialog("Thành công", 
                    "Đã setup biến môi trường thành công!\n\n" +
                    "Lần sau mở Unity sẽ tự động load password.", 
                    "OK");

                // Clear input fields
                keystorePassword = "";
                keyAliasPassword = "";
            }
            catch (Exception e)
            {
                Debug.LogError($"Lỗi khi setup biến môi trường: {e.Message}");
                EditorUtility.DisplayDialog("Lỗi", 
                    $"Không thể setup biến môi trường:\n{e.Message}", 
                    "OK");
            }
        }

        [FoldoutGroup("Keystore Settings")]
        [HorizontalGroup("Keystore Settings/Actions")]
        [Button("Load từ Environment", ButtonSizes.Medium)]
        [GUIColor(0.3f, 0.8f, 0.3f)]
        private void LoadKeystorePasswordFromEnv()
        {
            string keystorePass = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PASS", EnvironmentVariableTarget.User);
            string keyAliasPass = Environment.GetEnvironmentVariable("UNITY_KEY_ALIAS_PASS", EnvironmentVariableTarget.User);
            
            if (string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(keyAliasPass))
            {
                EditorUtility.DisplayDialog("Thông báo", 
                    "Chưa có biến môi trường!\n\n" +
                    "Vui lòng nhập password và bấm 'Setup Environment Variables'", 
                    "OK");
                return;
            }
            
            PlayerSettings.Android.keystorePass = keystorePass;
            PlayerSettings.Android.keyaliasPass = keyAliasPass;
            
            Debug.Log("✓ Đã load keystore password từ biến môi trường!");
        }

        [FoldoutGroup("Keystore Settings")]
        [HorizontalGroup("Keystore Settings/Actions")]
        [Button("Clear Environment", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.3f, 0.3f)]
        private void ClearEnvironmentVariables()
        {
            if (EditorUtility.DisplayDialog("Xác nhận", 
                "Bạn có chắc muốn xóa biến môi trường?", 
                "Xóa", "Hủy"))
            {
                Environment.SetEnvironmentVariable("UNITY_KEYSTORE_PASS", null, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("UNITY_KEY_ALIAS_PASS", null, EnvironmentVariableTarget.User);
                
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                
                Debug.Log("Đã xóa biến môi trường và keystore passwords");
            }
        }

        [FoldoutGroup("Keystore Settings")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Status")]
        private string EnvironmentStatus
        {
            get
            {
                string keystorePass = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PASS", EnvironmentVariableTarget.User);
                string keyAliasPass = Environment.GetEnvironmentVariable("UNITY_KEY_ALIAS_PASS", EnvironmentVariableTarget.User);
                
                if (!string.IsNullOrEmpty(keystorePass) && !string.IsNullOrEmpty(keyAliasPass))
                    return "✓ Đã setup biến môi trường";
                else
                    return "✗ Chưa setup biến môi trường";
            }
        }

        [FoldoutGroup("Keystore Settings")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("PlayerSettings Status")]
        private string PlayerSettingsStatus
        {
            get
            {
                if (!string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) && 
                    !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                    return "✓ Đã set password";
                else
                    return "✗ Chưa set password";
            }
        }

        private void AutoSetKeystorePasswordFromEnv()
        {
            string keystorePass = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PASS", EnvironmentVariableTarget.User);
            string keyAliasPass = Environment.GetEnvironmentVariable("UNITY_KEY_ALIAS_PASS", EnvironmentVariableTarget.User);
            
            if (!string.IsNullOrEmpty(keystorePass) && !string.IsNullOrEmpty(keyAliasPass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasPass = keyAliasPass;
                Debug.Log("✓ Keystore password đã được set tự động từ biến môi trường");
            }
        }

        #endregion
        
        #region Version Settings

        [FoldoutGroup("Version Settings")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Current Version Name")]
        private string CurrentVersionName => PlayerSettings.bundleVersion;

        [FoldoutGroup("Version Settings")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Current Version Code")]
        private int CurrentVersionCode => PlayerSettings.Android.bundleVersionCode;

        [FoldoutGroup("Version Settings")]
        [LabelText("New Version Name")]
        [SerializeField] private string newVersionName;

        [FoldoutGroup("Version Settings")]
        [LabelText("New Version Code")]
        [SerializeField] private int newVersionCode;

        [FoldoutGroup("Version Settings")]
        [Button("Set Version", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.6f, 1f)]
        private void SetVersion()
        {
            if (string.IsNullOrEmpty(newVersionName))
            {
                EditorUtility.DisplayDialog("Lỗi", 
                    "Vui lòng nhập Version Name!", 
                    "OK");
                return;
            }

            if (newVersionCode <= 0)
            {
                EditorUtility.DisplayDialog("Lỗi", 
                    "Version Code phải lớn hơn 0!", 
                    "OK");
                return;
            }

            PlayerSettings.bundleVersion = newVersionName;
            PlayerSettings.Android.bundleVersionCode = newVersionCode;

            Debug.Log($"✓ Đã set Version Name: {newVersionName}, Version Code: {newVersionCode}");
            EditorUtility.DisplayDialog("Thành công", 
                $"Đã cập nhật version:\nName: {newVersionName}\nCode: {newVersionCode}", 
                "OK");

            // Clear input
            newVersionName = "";
            newVersionCode = 0;
        }

        #endregion
        
        #region Scene Management

       
        private const string SCENE_ASPECT_KEY = "SceneAspectRatios";
        private const string AUTO_APPLY_ASPECT_KEY = "AutoApplySceneAspectRatios"; // Key lưu EditorPrefs
        
        [FoldoutGroup("Scene Management")]
        [ShowInInspector]
        [ToggleLeft]
        [LabelText("Auto Apply Aspect Ratio")]
        [InfoBox("Bật để tự động áp dụng Aspect Ratio khi đổi scene hoặc Play", InfoMessageType.None)]
        private bool AutoApplyAspectsEnabled
        {
            get => EditorPrefs.GetBool(AUTO_APPLY_ASPECT_KEY, true); // Mặc định là true nếu chưa set
            set => EditorPrefs.SetBool(AUTO_APPLY_ASPECT_KEY, value);
        }

        [FoldoutGroup("Scene Management")]
        [InfoBox("Danh sách các scene từ Build Settings", InfoMessageType.Info)]
        [OnInspectorGUI("DrawSceneList")]
        [ShowInInspector]
        private bool sceneManagementDummy;

        private void DrawSceneList()
        {
          var scenes = EditorBuildSettings.scenes;
    
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Scenes List", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
    
            bool isPlaying = EditorApplication.isPlaying || EditorApplication.isPaused;
    
            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                string displayName = scene.enabled ? $"[{i}] {sceneName}" : $"[{i}] {sceneName} (disabled)";
    
                EditorGUILayout.BeginHorizontal("box");
    
                EditorGUILayout.LabelField(displayName, GUILayout.Width(80));
    
                if (!sceneAspectRatios.ContainsKey(scene.path))
                {
                    sceneAspectRatios[scene.path] = "Free Aspect";
                }
    
                int currentIndex = Array.IndexOf(aspectRatioOptions, sceneAspectRatios[scene.path]);
                if (currentIndex == -1) currentIndex = 0;
    
                int newIndex = EditorGUILayout.Popup(currentIndex, aspectRatioOptions, GUILayout.Width(100));
                if (newIndex != currentIndex)
                {
                    sceneAspectRatios[scene.path] = aspectRatioOptions[newIndex];
                    SaveSceneAspectRatios();
                }
    
                GUILayout.FlexibleSpace();
    
                GUI.enabled = !isPlaying;
    
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Open", GUILayout.Width(80)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.path);
                        // Khi click tay từ bảng này, vẫn apply tỉ lệ cho scene đó tiện lợi
                        if (AutoApplyAspectsEnabled) ApplyAspectRatio(scene.path);
                        Debug.Log($"✓ Đã mở scene: {scene.path}");
                    }
                }
    
                GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
                if (GUILayout.Button("Play", GUILayout.Width(80)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.path);
                        if (AutoApplyAspectsEnabled) ApplyAspectRatio(scene.path);
                        EditorApplication.isPlaying = true;
                        Debug.Log($"✓ Playing scene: {scene.path}");
                    }
                }
    
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
    
                EditorGUILayout.EndHorizontal();
            }
    
            EditorGUILayout.Space(5);
        }


        private readonly string[] aspectRatioOptions = new string[]
        {
            "Free Aspect",
            "16:9",
            "16:10", 
            "9:16",
            "4:3",
            "5:4",
            "1:1"
        };

        private Dictionary<string, string> sceneAspectRatios = new Dictionary<string, string>();

        private void LoadSceneAspectRatios()
        {
            string json = EditorPrefs.GetString(SCENE_ASPECT_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<SceneAspectRatioList>(json);
                    sceneAspectRatios.Clear();
                    for (int i = 0; i < data.scenePaths.Count; i++)
                    {
                        sceneAspectRatios[data.scenePaths[i]] = data.aspectRatios[i];
                    }
                }
                catch
                {
                    sceneAspectRatios = new Dictionary<string, string>();
                }
            }
        }

        private void SaveSceneAspectRatios()
        {
            var data = new SceneAspectRatioList();
            foreach (var kvp in sceneAspectRatios)
            {
                data.scenePaths.Add(kvp.Key);
                data.aspectRatios.Add(kvp.Value);
            }
            string json = JsonUtility.ToJson(data);
            EditorPrefs.SetString(SCENE_ASPECT_KEY, json);
        }

        private void ApplyAspectRatio(string scenePath)
        {
            if (sceneAspectRatios.TryGetValue(scenePath, out string aspectRatio))
            {
                GameViewAspectRatioHelper.SetAspectRatio(aspectRatio);
                Debug.Log($"✓ Applied aspect ratio: {aspectRatio}");
            }
        }

        #endregion
        
        #region Database Management

        [FoldoutGroup("Database Management")]
        [Button("Refresh Database", ButtonSizes.Large)]
        [GUIColor(0.9f, 0.5f, 0.2f)]
        private void RefreshDatabase()
        {
            if (EditorUtility.DisplayDialog("Xác nhận", 
                    "Bạn có chắc muốn refresh database?\nHành động này có thể mất vài giây.", 
                    "Refresh", "Hủy"))
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Refreshing Database", "Đang refresh...", 0.5f);
            
                    // Refresh AssetDatabase
                    AssetDatabase.Refresh();
            
                    // Nếu bạn có custom database, gọi refresh method ở đây
                    // Example: YourDatabaseManager.Instance.Refresh();
            
                    EditorUtility.ClearProgressBar();
            
                    Debug.Log("✓ Database đã được refresh thành công!");
                    EditorUtility.DisplayDialog("Thành công", 
                        "Database đã được refresh!", 
                        "OK");
                }
                catch (Exception e)
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogError($"Lỗi khi refresh database: {e.Message}");
                    EditorUtility.DisplayDialog("Lỗi", 
                        $"Không thể refresh database:\n{e.Message}", 
                        "OK");
                }
            }
        }

        [FoldoutGroup("Database Management")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Last Refresh Time")]
        private string LastRefreshTime
        {
            get
            {
                // Có thể lưu thời gian refresh vào EditorPrefs
                string time = EditorPrefs.GetString("LastDatabaseRefresh", "Chưa refresh");
                return time;
            }
        }

        #endregion
    }
}