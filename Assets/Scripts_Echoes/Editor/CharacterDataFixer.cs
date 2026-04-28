using UnityEngine;
using UnityEditor;
using Echoes.Characters;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: 角色数据修复工具
    /// 用于重新序列化角色数据资产，解决 Inspector 显示问题
    /// </summary>
    public class CharacterDataFixer : EditorWindow
    {
        [MenuItem("Echoes/Fix Character Data Assets")]
        public static void FixCharacterDataAssets()
        {
            // 查找所有角色数据资产
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Resources/Characters" });
            
            int fixedCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                
                if (data != null)
                {
                    // 标记资产为脏，强制重新序列化
                    EditorUtility.SetDirty(data);
                    fixedCount++;
                    Debug.Log($"[CharacterDataFixer] Fixed: {path}");
                }
            }
            
            // 保存所有更改
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[CharacterDataFixer] Fixed {fixedCount} character data assets.");
            
            // 显示完成对话框
            EditorUtility.DisplayDialog(
                "Character Data Fixer",
                $"已修复 {fixedCount} 个角色数据资产。\n\n请在 Inspector 中重新选择资产查看字段。",
                "确定"
            );
        }
        
        [MenuItem("Echoes/Reimport Character Scripts")]
        public static void ReimportCharacterScripts()
        {
            // 重新导入角色相关脚本
            AssetDatabase.ImportAsset("Assets/Scripts_Echoes/Characters/CharacterData.cs", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Scripts_Echoes/Characters/CharacterManager.cs", ImportAssetOptions.ForceUpdate);
            
            Debug.Log("[CharacterDataFixer] Character scripts reimported.");
            
            EditorUtility.DisplayDialog(
                "Scripts Reimported",
                "角色脚本已重新导入。\n\n请等待编译完成后，在 Inspector 中重新选择资产。",
                "确定"
            );
        }
    }
}