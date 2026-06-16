using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ChieChie.Resource;
using Sirenix.OdinInspector.Editor;

namespace ChieChie.Core
{
    [CustomEditor(typeof(ResourceConfig))]
    public class ResourceConfigEditor : OdinEditor
    {
        // Lưu riêng trạng thái đóng/mở của Regen Settings theo Index để độc lập với thằng cha
        private Dictionary<int, bool> regenFoldoutStates = new Dictionary<int, bool>();

        public override void OnInspectorGUI()
        {
            ResourceConfig config = (ResourceConfig)target;

            serializedObject.Update();

            GUILayout.Label("Resource Configuration (Dictionary View)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Lấy danh sách gốc ngầm bên dưới
            var listProperty = serializedObject.FindProperty("resourcesList");

            // --- KHU VỰC NÚT ĐIỀU KHIỂN ĐÓNG/MỞ (CHỈ DÀNH CHO THẰNG CHA) ---
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft))
            {
                SetAllParentFoldersExpanded(listProperty, true);
            }
            
            if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight))
            {
                SetAllParentFoldersExpanded(listProperty, false);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            // ------------------------------------------

            // Định nghĩa Style cho chữ "(Regen)" để nó nổi bật cạnh tên Resource
            GUIStyle regenBadgeStyle = new GUIStyle(EditorStyles.miniLabel);
            regenBadgeStyle.normal.textColor = new Color(0.3f, 0.8f, 0.4f); // Màu xanh lá nhẹ dễ nhìn cả giao diện Light/Dark Mode
            regenBadgeStyle.fontStyle = FontStyle.Bold;
            regenBadgeStyle.margin = new RectOffset(5, 0, 2, 0); // Đẩy nhẹ khoảng cách ra xa tên một chút

            // Duyệt qua danh sách để vẽ từng cấu hình tài nguyên
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("key");
                SerializedProperty displayNameProp = element.FindPropertyRelative("displayName");
                SerializedProperty hasRegenProp = element.FindPropertyRelative("hasRegen");
                
                string enumName = keyProp.enumNames[keyProp.enumValueIndex];

                // Tạo một khối Box bao bọc riêng cho từng Item
                EditorGUILayout.BeginVertical("box");
                
                // --- THẰNG CHA: Layout ngang phối hợp Foldout + Status Text + Nút Xóa ---
                EditorGUILayout.BeginHorizontal();
                
                // Chuỗi hiển thị mặc định của tiêu đề Foldout
                string titleText = $"Resource: {enumName}";
                
                // Vẽ Foldout cha (nhưng dùng GUILayout.Width để khống chế độ rộng vừa đủ chữ, không chiếm hết hàng)
                Vector2 textSize = EditorStyles.foldoutHeader.CalcSize(new GUIContent(titleText));
                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, titleText, true, EditorStyles.foldoutHeader);

                // --- KIỂM TRA ĐIỀU KIỆN ĐỂ THÊM STATUS TEXT TẠI ĐÂY ---
                if (hasRegenProp.boolValue)
                {
                    GUILayout.Space(50);
                    GUILayout.Label("(Regen Active)", regenBadgeStyle);
                }

                GUILayout.FlexibleSpace(); // Đẩy nút Remove về sát rìa bên phải hàng

                // Đưa nút xóa lên trên thanh tiêu đề luôn cho gọn gàng và tiết kiệm không gian
                if (element.isExpanded)
                {
                    if (GUILayout.Button("Remove", GUILayout.Width(65)))
                    {
                        if (EditorUtility.DisplayDialog("Xác nhận xóa", $"Xóa cấu hình cho '{enumName}'?", "Xóa", "Hủy"))
                        {
                            listProperty.DeleteArrayElementAtIndex(i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Nếu thằng cha đang mở, thì mới hiển thị nội dung chi tiết bên trong
                if (element.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(2);

                    SerializedProperty maxStackProp = element.FindPropertyRelative("maxStack");
                    SerializedProperty defaultAmountProp = element.FindPropertyRelative("defaultAmount");

                    // Cấu hình chi tiết của nhóm Regen
                    SerializedProperty regenAmountProp = element.FindPropertyRelative("regenAmount");
                    SerializedProperty intervalSecondsProp = element.FindPropertyRelative("intervalSeconds");
                    SerializedProperty isEnabledByDefaultProp = element.FindPropertyRelative("isEnabledByDefault");

                    // --- PHẦN THÔNG TIN CƠ BẢN ---
                    float originalLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 120;

                    EditorGUILayout.PropertyField(keyProp, new GUIContent("Resource Type"));
                    EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Display Name"));
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(maxStackProp, new GUIContent("Max Stack"));
                    EditorGUILayout.PropertyField(defaultAmountProp, new GUIContent("Default"));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // --- PHẦN FOLDOUT REGENERATION SETTINGS (ĐỘC LẬP) ---
                    if (!regenFoldoutStates.ContainsKey(i)) regenFoldoutStates[i] = false;

                    regenFoldoutStates[i] = EditorGUILayout.Foldout(regenFoldoutStates[i], "Regeneration Settings", true);

                    if (regenFoldoutStates[i])
                    {
                        EditorGUILayout.BeginVertical("HelpBox");
                        
                        // Lắng nghe thay đổi khi bấm Checkbox Enable Regen để cập nhật Status Text ở trên ngay lập tức
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(hasRegenProp, new GUIContent("Enable Regen"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                        }
                        
                        if (hasRegenProp.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(regenAmountProp, new GUIContent("Regen Amount"));
                            EditorGUILayout.PropertyField(intervalSecondsProp, new GUIContent("Interval (Seconds)"));
                            EditorGUILayout.PropertyField(isEnabledByDefaultProp, new GUIContent("Enabled Default"));
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUIUtility.labelWidth = originalLabelWidth;
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical(); // Kết thúc Box của một Item
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Add New Resource Config", GUILayout.Height(30)))
            {
                listProperty.arraySize++; 
                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                newElement.isExpanded = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetAllParentFoldersExpanded(SerializedProperty listProperty, bool expand)
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                element.isExpanded = expand; 
            }
        }
    }
}