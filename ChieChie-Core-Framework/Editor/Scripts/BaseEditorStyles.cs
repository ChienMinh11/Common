using UnityEditor;
using UnityEngine;

namespace ChieChie.Editor
{
    public static class BaseEditorStyles
    {
        private static GUIStyle headerStyle;
        private static GUIStyle boxStyle;
        private static GUIStyle warningBoxStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle warningButtonStyle;
        private static GUIStyle errorButtonStyle;
        
        public static GUIStyle ButtonStyle
        {
            get
            {
                if (buttonStyle == null)
                {
                    buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Normal,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(10, 10, 5, 5)
                    };
                }
                return buttonStyle;
            }
        }
        public static GUIStyle WarningButtonStyle
        {
            get
            {
                if (warningButtonStyle == null)
                {
                    warningButtonStyle = new GUIStyle(ButtonStyle)
                    {
                        normal = { textColor = Color.yellow }
                    };
                }
                return warningButtonStyle;
            }
        }
        public static GUIStyle ErrorButtonStyle
        {
            get
            {
                if (errorButtonStyle == null)
                {
                    errorButtonStyle = new GUIStyle(ButtonStyle)
                    {
                        normal = { textColor = Color.red }
                    };
                }
                return errorButtonStyle;
            }
        }

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (headerStyle == null)
                {
                    headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 14,
                        margin = new RectOffset(5, 5, 10, 10)
                    };
                }
                return headerStyle;
            }
        }

        public static GUIStyle BoxStyle
        {
            get
            {
                if (boxStyle == null)
                {
                    boxStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(10, 10, 10, 10),
                        margin = new RectOffset(5, 5, 5, 5)
                    };
                }
                return boxStyle;
            }
        }

        public static GUIStyle WarningBoxStyle
        {
            get
            {
                if (warningBoxStyle == null)
                {
                    warningBoxStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(10, 10, 10, 10),
                        margin = new RectOffset(5, 5, 5, 5),
                        normal = { textColor = Color.yellow }
                    };
                }
                return warningBoxStyle;
            }
        }

        public static readonly float StandardButtonHeight = 30f;
        public static readonly float StandardSpacing = 10f;
        public static readonly float StandardIndent = 15f;
    }
}