using UnityEngine;
using UnityEditor;
using Echoes.Core;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: 视觉预制体生成器
    /// 在 Unity 编辑器中运行：菜单 -> Echoes -> Generate Visual Prefabs
    /// </summary>
    public class VisualPrefabGenerator : EditorWindow
    {
        [MenuItem("Echoes/Generate Visual Prefabs")]
        public static void GenerateVisualPrefabs()
        {
            string prefabPath = "Assets/Resources/Characters/Visuals/";
            
            // 确保目录存在
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Characters", "Visuals");
            }
            
            // 生成时光守望者视觉
            GenerateTimeKeeperVisual(prefabPath);
            
            // 生成凛冬静视觉
            GenerateIceFreezeVisual(prefabPath);
            
            // 生成焰舞红视觉
            GenerateFireDancerVisual(prefabPath);
            
            AssetDatabase.Refresh();
            Debug.Log("[VisualPrefabGenerator] Visual prefabs generated successfully!");
        }
        
        private static void GenerateTimeKeeperVisual(string path)
        {
            GameObject go = new GameObject("Visual_TimeKeeper");
            
            // 添加 SpriteRenderer
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(0.6f, 0.8f, 1f, 1f); // 蓝色
            spriteRenderer.sortingOrder = 2;
            
            // 添加 Animator（使用现有的动画控制器）
            var animator = go.AddComponent<Animator>();
            // 注意：需要在 Unity 中手动关联动画控制器
            
            // 保存预制体
            string prefabPath = path + "Visual_TimeKeeper.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[VisualPrefabGenerator] Created: {prefabPath}");
            
            DestroyImmediate(go);
        }
        
        private static void GenerateIceFreezeVisual(string path)
        {
            GameObject go = new GameObject("Visual_IceFreeze");
            
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(0.7f, 0.9f, 1f, 0.9f); // 冰蓝色半透明
            spriteRenderer.sortingOrder = 2;
            
            var animator = go.AddComponent<Animator>();
            
            string prefabPath = path + "Visual_IceFreeze.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[VisualPrefabGenerator] Created: {prefabPath}");
            
            DestroyImmediate(go);
        }
        
        private static void GenerateFireDancerVisual(string path)
        {
            GameObject go = new GameObject("Visual_FireDancer");
            
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(1f, 0.6f, 0.4f, 1f); // 橙红色
            spriteRenderer.sortingOrder = 2;
            
            var animator = go.AddComponent<Animator>();
            
            string prefabPath = path + "Visual_FireDancer.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[VisualPrefabGenerator] Created: {prefabPath}");
            
            DestroyImmediate(go);
        }
    }
}