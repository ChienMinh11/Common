using UnityEditor;
using UnityEngine;

namespace ChieChie.Editor
{
    public static class BaseEditorLayout
    {
        public static void BeginVerticalBox()
        {
            EditorGUILayout.BeginVertical(BaseEditorStyles.BoxStyle);
        }

        public static void EndVerticalBox()
        {
            EditorGUILayout.EndVertical();
        }

        public static void DrawHeader(string text)
        {
            EditorGUILayout.LabelField(text, BaseEditorStyles.HeaderStyle);
        }

        public static void DrawSectionHeader(string text)
        {
            EditorGUILayout.Space(BaseEditorStyles.StandardSpacing);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        public static void DrawHelpBox(string message, MessageType type)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        public static bool DrawButton(string text)
        {
            return GUILayout.Button(text, GUILayout.Height(BaseEditorStyles.StandardButtonHeight));
        }

        public static void DrawSpace()
        {
            EditorGUILayout.Space(BaseEditorStyles.StandardSpacing);
        }

        public static bool DrawToggleField(string label, bool value, string tooltip = null)
        {
            GUIContent content = new GUIContent(label, tooltip);
            return EditorGUILayout.Toggle(content, value);
        }

        public static string DrawTextField(string label, string value, string tooltip = null)
        {
            GUIContent content = new GUIContent(label, tooltip);
            return EditorGUILayout.TextField(content, value);
        }

        public static float DrawSliderField(string label, float value, float min, float max, string tooltip = null)
        {
            GUIContent content = new GUIContent(label, tooltip);
            return EditorGUILayout.Slider(content, value, min, max);
        }

        public static int DrawIntSliderField(string label, int value, int min, int max, string tooltip = null)
        {
            GUIContent content = new GUIContent(label, tooltip);
            return EditorGUILayout.IntSlider(content, value, min, max);
        }
      
        
    }
}