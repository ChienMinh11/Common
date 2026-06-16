using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
// Thêm namespace này để dùng NamedBuildTarget

namespace ChieChie.Editor
{
#if UNITY_EDITOR
    public class DefineManagerData
    {
        private static DefineManagerData instance;
        public static DefineManagerData Instance
        {
            get
            {
                if (instance == null)
                    instance = new DefineManagerData();
                return instance;
            }
        }

        private Dictionary<string, bool> defineStates = new Dictionary<string, bool>();
        private readonly string[] predefinedDefines = new string[]
        {
            "SPINE_UNITY",
            "USE_ADSMODULE",
            "UNITASK_DOTWEEN_SUPPORT"
        };

        public IEnumerable<string> PredefinedDefines => predefinedDefines;
    
        public IEnumerable<string> CustomDefines 
        {
            get 
            {
                return defineStates.Keys.Where(key => !predefinedDefines.Contains(key));
            }
        }
    
        public Dictionary<string, bool> DefineStates => defineStates;

        public void RefreshDefines(BuildTargetGroup platform)
        {
            // Chuyển đổi BuildTargetGroup sang NamedBuildTarget cho Unity 6
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(platform);
            
            // Sử dụng hàm mới của Unity 6 để lấy định nghĩa
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var currentDefines = defines.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToList();
        
            defineStates.Clear();
        
            foreach (var define in currentDefines)
            {
                defineStates[define] = true;
            }
        
            foreach (var define in predefinedDefines)
            {
                if (!defineStates.ContainsKey(define))
                {
                    defineStates[define] = false;
                }
            }
        }

        public void SaveDefines(BuildTargetGroup platform)
        {
            var activeDefines = defineStates.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
            string defines = string.Join(";", activeDefines);
            
            // Chuyển đổi BuildTargetGroup sang NamedBuildTarget cho Unity 6
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(platform);
            
            // Sử dụng hàm mới của Unity 6 để lưu định nghĩa
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
        
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.Refresh();
                RefreshDefines(platform);
            };
        }

        public void SetDefineState(string define, bool state, BuildTargetGroup platform)
        {
            if (!defineStates.ContainsKey(define))
            {
                defineStates[define] = state;
            }
            else if (defineStates[define] != state)
            {
                defineStates[define] = state;
            }
            SaveDefines(platform);
        }
    }

    public class DefineManager : EditorWindow
    {
        private Vector2 scrollPosition;
        private BuildTargetGroup selectedPlatform = BuildTargetGroup.Standalone;

        [MenuItem("CORE/Define Manager")]
        public static void ShowWindow()
        {
            GetWindow<DefineManager>("Define Manager");
        }

        private void OnEnable()
        {
            DefineManagerData.Instance.RefreshDefines(selectedPlatform);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
        
            EditorGUILayout.BeginHorizontal();
        
            EditorGUI.BeginChangeCheck();
            selectedPlatform = (BuildTargetGroup)EditorGUILayout.EnumPopup("Platform", selectedPlatform);
            if (EditorGUI.EndChangeCheck())
            {
                DefineManagerData.Instance.RefreshDefines(selectedPlatform);
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                DefineManagerData.Instance.RefreshDefines(selectedPlatform);
            }
        
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Predefined Defines:", EditorStyles.boldLabel);
        
            foreach (var define in DefineManagerData.Instance.PredefinedDefines)
            {
                GUILayout.BeginHorizontal();
            
                bool currentState = DefineManagerData.Instance.DefineStates[define];
                bool newState = EditorGUILayout.Toggle(currentState, GUILayout.Width(20));
                if (newState != currentState)
                {
                    DefineManagerData.Instance.SetDefineState(define, newState, selectedPlatform);
                }
            
                EditorGUILayout.LabelField(define);
            
                GUILayout.EndHorizontal();
            }

            var customDefines = DefineManagerData.Instance.CustomDefines;
            if (customDefines.Any())
            {
                GUILayout.Space(20);
                GUILayout.Label("Custom Defines (Read-only):", EditorStyles.boldLabel);
            
                EditorGUI.BeginDisabledGroup(true);
            
                foreach (var define in customDefines)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.Toggle(DefineManagerData.Instance.DefineStates[define], GUILayout.Width(20));
                    EditorGUILayout.LabelField(define);
                    GUILayout.EndHorizontal();
                }
            
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndScrollView();
        }
    }
#endif
}