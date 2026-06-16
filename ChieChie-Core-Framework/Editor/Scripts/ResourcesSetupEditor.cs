using System.IO;
using ChieChie.Core;
using UnityEditor;
using UnityEngine;

namespace ChieChie.Editor
{
    public class ResourcesSetupEditor : EditorWindow
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string ConfigSubFolderName = "Config";
        
        private static readonly string ConfigFolderPath = Path.Combine(ResourcesFolderPath, ConfigSubFolderName).Replace('\\', '/');

        [MenuItem("CORE/Fast Setup/Create Resources & Config")]
        public static void QuickSetup()
        {
           
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Xác nhận khởi tạo",
                "Bạn có chắc chắn muốn thiết lập hệ thống Resources và tự động tạo các file Config không?",
                "Đồng ý thiết lập",
                "Hủy bỏ"
            );
          
            if (!isConfirmed)
            {
                Debug.Log("<b>[Setup]</b> Đã hủy quá trình thiết lập theo yêu cầu của bạn.");
                return;
            }

            
            if (!AssetDatabase.IsValidFolder(ResourcesFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
                Debug.Log($"<b>[Setup]</b> Đã tạo thư mục gốc: {ResourcesFolderPath}");
            }
         
            if (!AssetDatabase.IsValidFolder(ConfigFolderPath))
            {
                AssetDatabase.CreateFolder(ResourcesFolderPath, ConfigSubFolderName);
                Debug.Log($"<b>[Setup]</b> Đã tạo thư mục con: {ConfigFolderPath}");
            }
         
            // CreateConfigAsset<InitialisationConfig>("InitialisationConfig.asset");
            // CreateConfigAsset<AudioConfig>("AudioConfig.asset");
            // CreateConfigAsset<VibrationConfig>("VibrationConfig.asset");
            // CreateConfigAsset<ResourceConfig>("ResourceConfig.asset");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("<b>[Setup]</b> <color=cyan>Hệ thống cấu hình đã được thiết lập hoàn tất trong thư mục Config!</color>");
        }

       
        private static void CreateConfigAsset<T>(string fileName) where T : ScriptableObject
        {
            string fullPath = Path.Combine(ConfigFolderPath, fileName).Replace('\\', '/');

            if (AssetDatabase.LoadAssetAtPath<T>(fullPath) == null)
            {
                T asset = ScriptableObject.CreateInstance<T>();
                
               
                AssetDatabase.CreateAsset(asset, fullPath);
                
                Debug.Log($"<b>[Setup]</b> Đã tạo thành công cấu hình tại: <color=green>{fullPath}</color>");
            }
            else
            {
                Debug.Log($"<b>[Setup]</b> File <color=yellow>{fileName}</color> đã tồn tại trong thư mục Config. Bỏ qua để bảo vệ dữ liệu cũ.");
            }
        }
    }
}