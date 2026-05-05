using UnityEngine;
using UnityEditor;
using Level;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: LevelData 资产批量生成器
    /// 菜单 -> Echoes -> Generate Level Data Assets
    /// 为 level0.txt ~ level9.txt 自动创建对应的 LevelData ScriptableObject
    /// 生成后在 LevelLoader 的 Level Data Array 字段中按顺序拖入即可
    /// </summary>
    public class LevelDataGenerator : EditorWindow
    {
        // 关卡元数据配置表
        private static readonly (string file, string name, int diff, int par)[] Meta =
        {
            ("level0", "心域序章",   1, 15),
            ("level1", "初识回响",   1, 25),
            ("level2", "双重镜像",   2, 30),
            ("level3", "时光碎片",   2, 35),
            ("level4", "冰封记忆",   3, 40),
            ("level5", "焰舞轨迹",   3, 45),
            ("level6", "递归之门",   4, 50),
            ("level7", "心魔回廊",   4, 55),
            ("level8", "幻匣深处",   5, 60),
            ("level9", "回响终章",   5, 70),
        };

        [MenuItem("Echoes/Generate Level Data Assets")]
        public static void Generate()
        {
            const string outDir = "Assets/Resources/Levels/LevelData";

            if (!AssetDatabase.IsValidFolder(outDir))
                AssetDatabase.CreateFolder("Assets/Resources/Levels", "LevelData");

            int created = 0;

            for (int i = 0; i < Meta.Length; i++)
            {
                var (file, name, diff, par) = Meta[i];

                // 加载对应的 TextAsset（原有 levels[] 中的文件）
                TextAsset txt = Resources.Load<TextAsset>($"Levels/{file}");
                if (txt == null)
                {
                    Debug.LogWarning($"[LevelDataGenerator] TextAsset not found: Levels/{file}");
                    continue;
                }

                string path = $"{outDir}/LevelData_{i:D2}.asset";

                // 已存在则跳过，避免覆盖手动编辑的数据
                if (AssetDatabase.LoadAssetAtPath<LevelData>(path) != null)
                {
                    Debug.Log($"[LevelDataGenerator] Skipped (already exists): {path}");
                    continue;
                }

                LevelData data = ScriptableObject.CreateInstance<LevelData>();
                SerializedObject so = new SerializedObject(data);

                so.FindProperty("levelIndex").intValue              = i;
                so.FindProperty("levelName").stringValue            = name;
                so.FindProperty("description").stringValue          = $"第 {i + 1} 关";
                so.FindProperty("levelText").objectReferenceValue   = txt;
                so.FindProperty("difficulty").intValue              = diff;
                so.FindProperty("parMoves").intValue                = par;
                so.FindProperty("unlockedByDefault").boolValue      = (i == 0);
                so.FindProperty("requiredLevelIndex").intValue      = i == 0 ? -1 : i - 1;
                so.ApplyModifiedProperties();

                AssetDatabase.CreateAsset(data, path);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Level Data Generated",
                $"已生成 {created} 个 LevelData 资产。\n\n" +
                $"路径：{outDir}/\n\n" +
                "请在 LevelLoader 的\n「Level Data Array」字段中\n按顺序拖入 LevelData_00 ~ LevelData_09。",
                "确定");
        }
    }
}
