using UnityEngine;
using UnityEditor;
using Echoes.Core;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: 技能预制体生成器
    /// 在 Unity 编辑器中运行：菜单 -> Echoes -> Generate Skill Prefabs
    /// </summary>
    public class SkillPrefabGenerator : EditorWindow
    {
        [MenuItem("Echoes/Generate Skill Prefabs")]
        public static void GenerateSkillPrefabs()
        {
            string prefabPath = "Assets/Resources/Characters/Skills/";
            
            // 确保目录存在
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Characters", "Skills");
            }
            
            // 生成时间回溯技能预制体
            GenerateTimeRewindPrefab(prefabPath);
            
            // 生成冰封固化技能预制体
            GenerateIceFreezePrefab(prefabPath);
            
            // 生成爆裂推进技能预制体
            GenerateBurstPushPrefab(prefabPath);
            
            AssetDatabase.Refresh();
            Debug.Log("[SkillPrefabGenerator] Skill prefabs generated successfully!");
        }
        
        private static void GenerateTimeRewindPrefab(string path)
        {
            GameObject go = new GameObject("Skill_TimeRewind");
            
            // 添加技能组件
            var skill = go.AddComponent<TimeRewindSkill>();
            skill.SetField("skillName", "时间回溯");
            skill.SetField("energyCost", 1);
            skill.SetField("isUnlocked", true);
            skill.SetField("playEffectOnRewind", true);
            
            // 保存预制体
            string prefabPath = path + "Skill_TimeRewind.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[SkillPrefabGenerator] Created: {prefabPath}");
            
            DestroyImmediate(go);
        }
        
        private static void GenerateIceFreezePrefab(string path)
        {
            GameObject go = new GameObject("Skill_IceFreeze");
            
            // 添加技能组件
            var skill = go.AddComponent<IceFreezeSkill>();
            skill.SetField("skillName", "冰封固化");
            skill.SetField("energyCost", 2);
            skill.SetField("isUnlocked", true);
            skill.SetField("freezeRange", 1.5f);
            
            // 保存预制体
            string prefabPath = path + "Skill_IceFreeze.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[SkillPrefabGenerator] Created: {prefabPath}");
            
            DestroyImmediate(go);
        }
        
        private static void GenerateBurstPushPrefab(string path)
        {
            GameObject go = new GameObject("Skill_BurstPush");
            
            // 添加技能组件
            var skill = go.AddComponent<BurstPushSkill>();
            skill.SetField("skillName", "爆裂推进");
            skill.SetField("energyCost", 2);
            skill.SetField("isUnlocked", true);
            skill.SetField("pushDistance", 3f);
            
            // 保存预制体
            string prefabPath = path + "Skill_BurstPush.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Debug.Log($"[SkillPrefabGenerator] Created: {prefabPath}");
            
            DestroyImmediate(go);
        }
    }
    
    // 扩展方法用于设置私有字段
    public static class SkillExtensions
    {
        public static void SetField(this MonoBehaviour behaviour, string fieldName, object value)
        {
            var field = behaviour.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.FlattenHierarchy);
            
            if (field != null)
            {
                field.SetValue(behaviour, value);
            }
        }
    }
}