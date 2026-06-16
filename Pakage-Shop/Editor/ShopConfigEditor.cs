using System.Collections.Generic;
using ChieChie.Core;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ChieChie.Shop.Editor
{
    [CustomEditor(typeof(ShopConfig))]
    public class ShopConfigEditor : OdinEditor
    {
        private Dictionary<int, bool> rewardFoldoutStates = new Dictionary<int, bool>();

        public override void OnInspectorGUI()
        {
            var shopConfig = (ShopConfig)target;
            serializedObject.Update();

            GUILayout.Label("Shop Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            var listProperty = serializedObject.FindProperty("shopItems");
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft))
                SetAllParentFoldersExpanded(listProperty, true);
            if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight))
                SetAllParentFoldersExpanded(listProperty, false);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProp = element.FindPropertyRelative("productID");

                string enumName = keyProp.enumNames[keyProp.enumValueIndex];

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                string titleText = $"Pack: {enumName}";

                Vector2 textSize = EditorStyles.foldoutHeader.CalcSize(new GUIContent(titleText));
                element.isExpanded =
                    EditorGUILayout.Foldout(element.isExpanded, titleText, true, EditorStyles.foldoutHeader);

                GUILayout.FlexibleSpace();

                if (element.isExpanded)
                {
                    if (GUILayout.Button("Remove", GUILayout.Width(65)))
                    {
                        if (EditorUtility.DisplayDialog("Xác nhận xóa", $"Xóa cấu hình cho '{enumName}'?", "Xóa",
                                "Hủy"))
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
                    float originalLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 140; 

                    // --- [1] CẤU HÌNH CHI TIẾT CỦA SHOP ITEM DATA ---
                    EditorGUILayout.LabelField("General Info", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("productID"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("displayName"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("description"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("icon"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("isOneTimePurchase"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("isTimeLimited"));

                    EditorGUILayout.Space(10);

                    // --- [2] PHẦN FOLDOUT DANH SÁCH PHẦN THƯỞNG (REWARDS) ---
                    if (!rewardFoldoutStates.ContainsKey(i)) rewardFoldoutStates[i] = false;

                    rewardFoldoutStates[i] = EditorGUILayout.Foldout(rewardFoldoutStates[i], "Rewards Settings", true);

                    if (rewardFoldoutStates[i])
                    {
                        EditorGUILayout.BeginVertical("HelpBox");

                        SerializedProperty rewardsProp = element.FindPropertyRelative("rewards");
                        
                        // Header quản lý phần thưởng
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label($"Rewards List ({rewardsProp.arraySize})", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("+ Add Reward", GUILayout.Width(100)))
                        {
                            rewardsProp.arraySize++;
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUILayout.Space(5);

                        // Duyệt qua từng phần thưởng trong danh sách rewards của Pack
                        for (int j = 0; j < rewardsProp.arraySize; j++)
                        {
                            SerializedProperty rewardElement = rewardsProp.GetArrayElementAtIndex(j);
                            
                            EditorGUILayout.BeginVertical("box");
                            EditorGUILayout.BeginHorizontal();
                            
                            // Tạo tiêu đề nhỏ hiển thị Loại phần thưởng và tài nguyên
                            SerializedProperty rewardTypeProp = rewardElement.FindPropertyRelative("rewardType");
                            SerializedProperty resTypeProp = rewardElement.FindPropertyRelative("resourceType");
                            string rewardTypeName = rewardTypeProp.enumNames[rewardTypeProp.enumValueIndex];
                            string resTypeName = resTypeProp.enumNames[resTypeProp.enumValueIndex];
                            
                            GUILayout.Label($"[Element {j}] {rewardTypeName} -> {resTypeName}", EditorStyles.miniBoldLabel);
                            
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("X", GUILayout.Width(25)))
                            {
                                rewardsProp.DeleteArrayElementAtIndex(j);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();
                                break; 
                            }
                            EditorGUILayout.EndHorizontal();

                            // Hiển thị các trường từ ShopItemReward (lớp con)
                            EditorGUILayout.PropertyField(rewardTypeProp);
                            
                            // Hiển thị các trường từ BaseRewardData (lớp cha)
                            EditorGUILayout.PropertyField(resTypeProp);
                            
                            SerializedProperty isInfiniteProp = rewardElement.FindPropertyRelative("isInfiniteReward");
                            EditorGUILayout.PropertyField(isInfiniteProp);
                            
                            // Logic ẩn/hiện thông minh: Vô hạn thì hiện thời gian, hữu hạn thì hiện số lượng
                            if (isInfiniteProp.boolValue)
                            {
                                EditorGUILayout.PropertyField(rewardElement.FindPropertyRelative("infinityDuration"));
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(rewardElement.FindPropertyRelative("amount"));
                            }

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(2);
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUIUtility.labelWidth = originalLabelWidth;
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical(); // Kết thúc Box của một Item
                EditorGUILayout.Space(2);
                EditorGUILayout.Space();
            }
            
            if (GUILayout.Button("Add New Pack Config", GUILayout.Height(30)))
            {
                listProperty.arraySize++;
                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
    
                // 1. Mở rộng phần tử mới tạo
                newElement.isExpanded = true;

                // 2. Reset các trường dữ liệu cơ bản của Shop Item về mặc định
                newElement.FindPropertyRelative("productID").enumValueIndex = 0;
                newElement.FindPropertyRelative("displayName").stringValue = string.Empty;
                newElement.FindPropertyRelative("description").stringValue = string.Empty;
                newElement.FindPropertyRelative("icon").objectReferenceValue = null;
                newElement.FindPropertyRelative("isOneTimePurchase").boolValue = false;
                newElement.FindPropertyRelative("isTimeLimited").boolValue = false;

                // 3. Xóa sạch danh sách phần thưởng (Rewards) đi kèm nếu có, tránh copy từ thằng cũ
                SerializedProperty newRewardsProp = newElement.FindPropertyRelative("rewards");
                if (newRewardsProp != null)
                {
                    newRewardsProp.ClearArray(); // Đưa số lượng phần thưởng của Pack mới về 0
                }
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