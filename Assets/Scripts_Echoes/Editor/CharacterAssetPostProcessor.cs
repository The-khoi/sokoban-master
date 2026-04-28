using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Echoes.Characters;
using Echoes.Core;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: 角色资产同步流水线
    /// 菜单路径：Echoes / Pipeline / Sync Character Assets
    ///
    /// 功能：
    ///   1. 遍历所有 CharacterData.asset，检查并补全 Visual 预制体
    ///   2. 检查并补全 Skill 预制体（依据 SkillConfig 中的 SkillType 动态推导类名）
    ///   3. 将生成的预制体引用自动写回 CharacterData，并保存资产
    ///
    /// 设计原则：
    ///   - 严禁硬编码路径，全部使用 Path.Combine + 常量
    ///   - 幂等性：已存在的资产不会被覆盖，只补全缺失项
    ///   - 详细日志：每一步操作均输出带前缀的日志，方便追溯
    /// </summary>
    public static class CharacterAssetPostProcessor
    {
        #region Path Constants

        // ── 根目录 ──────────────────────────────────────────────────────────
        private const string CHARACTERS_ROOT   = "Assets/Resources/Characters";
        private const string VISUALS_FOLDER    = "Visuals";
        private const string SKILLS_FOLDER     = "Skills";

        // ── 脚本搜索目录 ─────────────────────────────────────────────────────
        private const string SKILLS_SCRIPTS_ROOT = "Assets/Scripts_Echoes/Skills";

        // ── 命名规则 ─────────────────────────────────────────────────────────
        private const string VISUAL_PREFIX = "Visual_";
        private const string SKILL_PREFIX  = "Skill_";
        private const string SKILL_SUFFIX  = "Skill";   // SkillType.TimeRewind → "TimeRewindSkill"

        // ── 日志前缀 ─────────────────────────────────────────────────────────
        private const string LOG_TAG = "[CharacterAssetPostProcessor]";

        #endregion

        #region Menu Entry

        /// <summary>
        /// 菜单入口：Echoes / Pipeline / Sync Character Assets
        /// </summary>
        [MenuItem("Echoes/Pipeline/Sync Character Assets")]
        public static void SyncCharacterAssets()
        {
            Debug.Log($"{LOG_TAG} ══════════ 开始同步角色资产 ══════════");

            // ── 1. 确保目标目录存在 ──────────────────────────────────────────
            EnsureFolder(CHARACTERS_ROOT, VISUALS_FOLDER);
            EnsureFolder(CHARACTERS_ROOT, SKILLS_FOLDER);

            // ── 2. 收集所有 CharacterData ────────────────────────────────────
            string[] guids = AssetDatabase.FindAssets(
                "t:CharacterData",
                new[] { CHARACTERS_ROOT }
            );

            if (guids.Length == 0)
            {
                Debug.LogWarning($"{LOG_TAG} 未找到任何 CharacterData 资产，请检查路径：{CHARACTERS_ROOT}");
                return;
            }

            Debug.Log($"{LOG_TAG} 共找到 {guids.Length} 个 CharacterData 资产，开始逐一处理…");

            // ── 3. 统计数据 ──────────────────────────────────────────────────
            int visualCreated  = 0;
            int skillCreated   = 0;
            int assetSaved     = 0;

            // ── 4. 逐个处理 ──────────────────────────────────────────────────
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);

                if (data == null)
                {
                    Debug.LogWarning($"{LOG_TAG} 无法加载资产：{assetPath}，已跳过。");
                    continue;
                }

                Debug.Log($"{LOG_TAG} ── 处理角色：{data.CharacterName} (ID={data.CharacterId}) ──");

                bool dirty = false;

                // 4a. 同步 Visual 预制体
                if (SyncVisualPrefab(data, assetPath, out GameObject visualPrefab))
                {
                    visualCreated++;
                    dirty = true;
                }

                // 4b. 同步 Skill 预制体（逐条 SkillConfig）
                if (SyncSkillPrefabs(data, assetPath))
                {
                    skillCreated++;   // 此处为"至少新建了一个技能预制体"的角色计数
                    dirty = true;
                }

                // 4c. 保存修改后的 CharacterData
                if (dirty)
                {
                    EditorUtility.SetDirty(data);
                    assetSaved++;
                }
            }

            // ── 5. 统一保存 & 刷新 ──────────────────────────────────────────
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── 6. 汇总报告 ──────────────────────────────────────────────────
            string summary =
                $"同步完成！\n\n" +
                $"• 新建 Visual 预制体：{visualCreated} 个\n" +
                $"• 新建 Skill 预制体（含角色数）：{skillCreated} 个\n" +
                $"• 已更新 CharacterData：{assetSaved} 个";

            Debug.Log($"{LOG_TAG} ══════════ 同步完成 ══════════\n{summary}");

            EditorUtility.DisplayDialog("Sync Character Assets", summary, "确定");
        }

        #endregion

        #region Visual Prefab Sync

        /// <summary>
        /// 检查并补全指定角色的 Visual 预制体。
        /// 若预制体已存在且 CharacterData 已引用，则跳过。
        /// </summary>
        /// <param name="data">目标角色数据</param>
        /// <param name="dataAssetPath">CharacterData 资产路径（用于日志）</param>
        /// <param name="outPrefab">输出：新建或已有的预制体（未使用时为 null）</param>
        /// <returns>是否对 CharacterData 做了修改</returns>
        private static bool SyncVisualPrefab(
            CharacterData data,
            string dataAssetPath,
            out GameObject outPrefab)
        {
            outPrefab = null;

            string visualsDir  = Path.Combine(CHARACTERS_ROOT, VISUALS_FOLDER);
            string prefabName  = $"{VISUAL_PREFIX}{data.CharacterName}.prefab";
            string prefabPath  = Path.Combine(visualsDir, prefabName)
                                     .Replace('\\', '/');   // Unity 要求正斜杠

            // ── 情况 A：CharacterData 已有引用且预制体文件存在 ────────────────
            if (data.VisualPrefab != null)
            {
                string existingPath = AssetDatabase.GetAssetPath(data.VisualPrefab);
                if (File.Exists(existingPath))
                {
                    Debug.Log($"{LOG_TAG}   [Visual] ✓ 已存在引用：{existingPath}，跳过。");
                    return false;
                }

                // 引用存在但文件丢失，继续重建
                Debug.LogWarning($"{LOG_TAG}   [Visual] ⚠ 引用指向的文件已丢失（{existingPath}），将重新生成。");
            }

            // ── 情况 B：预制体文件已存在但 CharacterData 未引用 ──────────────
            if (File.Exists(prefabPath))
            {
                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existing != null)
                {
                    Debug.Log($"{LOG_TAG}   [Visual] ✓ 预制体文件已存在，补全引用：{prefabPath}");
                    WriteVisualPrefabRef(data, dataAssetPath, existing);
                    outPrefab = existing;
                    return true;
                }
            }

            // ── 情况 C：需要新建预制体 ────────────────────────────────────────
            Debug.Log($"{LOG_TAG}   [Visual] ✦ 新建预制体：{prefabPath}");

            GameObject go = new GameObject($"{VISUAL_PREFIX}{data.CharacterName}");

            // SpriteRenderer（颜色按角色 ID 区分，与 VisualPrefabGenerator 保持一致）
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color        = GetVisualColor(data.CharacterId);
            sr.sortingOrder = 2;

            // Animator（控制器需在 Editor 中手动关联）
            go.AddComponent<Animator>();

            // 保存为预制体
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);

            if (prefab == null)
            {
                Debug.LogError($"{LOG_TAG}   [Visual] ✗ 预制体保存失败：{prefabPath}");
                return false;
            }

            Debug.Log($"{LOG_TAG}   [Visual] ✦ 已保存：{prefabPath}");

            // 写回引用
            WriteVisualPrefabRef(data, dataAssetPath, prefab);
            outPrefab = prefab;
            return true;
        }

        /// <summary>
        /// 通过 SerializedObject 将 Visual 预制体引用写入 CharacterData 的私有字段。
        /// </summary>
        private static void WriteVisualPrefabRef(
            CharacterData data,
            string dataAssetPath,
            GameObject prefab)
        {
            SerializedObject   so   = new SerializedObject(data);
            SerializedProperty prop = so.FindProperty("visualPrefab");

            if (prop == null)
            {
                Debug.LogError($"{LOG_TAG}   [Visual] ✗ 找不到字段 'visualPrefab'，请检查 CharacterData.cs 中的字段名。");
                return;
            }

            so.Update();
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"{LOG_TAG}   [Visual] ✓ 已将引用写入 {dataAssetPath}");
        }

        /// <summary>
        /// 根据角色 ID 返回对应的视觉颜色（与 VisualPrefabGenerator 保持一致）。
        /// </summary>
        private static Color GetVisualColor(int characterId)
        {
            return characterId switch
            {
                1 => new Color(0.6f, 0.8f, 1.0f, 1.0f),   // TimeKeeper  — 蓝色
                2 => new Color(0.7f, 0.9f, 1.0f, 0.9f),   // IceFreeze   — 冰蓝半透明
                3 => new Color(1.0f, 0.6f, 0.4f, 1.0f),   // FireDancer  — 橙红色
                _ => Color.white
            };
        }

        #endregion

        #region Skill Prefab Sync

        /// <summary>
        /// 检查并补全指定角色所有 SkillConfig 对应的 Skill 预制体。
        /// </summary>
        /// <returns>是否对 CharacterData 做了修改（至少新建/补全了一个技能预制体引用）</returns>
        private static bool SyncSkillPrefabs(CharacterData data, string dataAssetPath)
        {
            if (data.Skills == null || data.Skills.Length == 0)
            {
                Debug.Log($"{LOG_TAG}   [Skill] 角色 {data.CharacterName} 无技能配置，跳过。");
                return false;
            }

            bool anyDirty = false;

            // SerializedObject 用于读写 SkillConfig 数组（struct 私有字段）
            SerializedObject   so         = new SerializedObject(data);
            SerializedProperty skillsArr  = so.FindProperty("skills");

            if (skillsArr == null || !skillsArr.isArray)
            {
                Debug.LogError($"{LOG_TAG}   [Skill] ✗ 找不到字段 'skills'，请检查 CharacterData.cs 中的字段名。");
                return false;
            }

            so.Update();

            for (int i = 0; i < skillsArr.arraySize; i++)
            {
                SerializedProperty configProp    = skillsArr.GetArrayElementAtIndex(i);
                SerializedProperty skillTypeProp = configProp.FindPropertyRelative("skillType");
                SerializedProperty prefabProp    = configProp.FindPropertyRelative("skillPrefab");

                if (skillTypeProp == null || prefabProp == null)
                {
                    Debug.LogWarning($"{LOG_TAG}   [Skill] ⚠ SkillConfig[{i}] 字段缺失，跳过。");
                    continue;
                }

                SkillType skillType = (SkillType)skillTypeProp.enumValueIndex;

                // SkillType.None 不需要预制体
                if (skillType == SkillType.None)
                {
                    Debug.Log($"{LOG_TAG}   [Skill] SkillConfig[{i}] 类型为 None，跳过。");
                    continue;
                }

                // 推导类名：TimeRewind → TimeRewindSkill
                string className  = $"{skillType}{SKILL_SUFFIX}";
                string prefabName = $"{SKILL_PREFIX}{skillType}.prefab";
                string prefabPath = Path.Combine(CHARACTERS_ROOT, SKILLS_FOLDER, prefabName)
                                        .Replace('\\', '/');

                Debug.Log($"{LOG_TAG}   [Skill] 处理 SkillConfig[{i}]：{skillType} → 类名={className}，预制体={prefabPath}");

                // ── 情况 A：已有引用且文件存在 ──────────────────────────────
                if (prefabProp.objectReferenceValue != null)
                {
                    string existingPath = AssetDatabase.GetAssetPath(prefabProp.objectReferenceValue);
                    if (File.Exists(existingPath))
                    {
                        Debug.Log($"{LOG_TAG}   [Skill] ✓ 已存在引用：{existingPath}，跳过。");
                        continue;
                    }
                    Debug.LogWarning($"{LOG_TAG}   [Skill] ⚠ 引用指向的文件已丢失（{existingPath}），将重新生成。");
                }

                // ── 情况 B：文件已存在但未引用 ──────────────────────────────
                if (File.Exists(prefabPath))
                {
                    GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (existing != null)
                    {
                        Debug.Log($"{LOG_TAG}   [Skill] ✓ 预制体文件已存在，补全引用：{prefabPath}");
                        prefabProp.objectReferenceValue = existing;
                        anyDirty = true;
                        continue;
                    }
                }

                // ── 情况 C：需要新建预制体 ────────────────────────────────────
                // 通过反射在运行时程序集中查找对应的 Skill 子类
                Type skillComponentType = FindSkillType(className);

                if (skillComponentType == null)
                {
                    Debug.LogWarning(
                        $"{LOG_TAG}   [Skill] ⚠ 未找到类型 '{className}'（搜索范围：所有已加载程序集）。" +
                        $"请确认脚本已编译，或手动创建预制体后重新运行同步。跳过。"
                    );
                    continue;
                }

                Debug.Log($"{LOG_TAG}   [Skill] ✦ 新建预制体：{prefabPath}（组件：{className}）");

                GameObject go = new GameObject($"{SKILL_PREFIX}{skillType}");

                // 挂载 Skill 子类组件
                Component skillComp = go.AddComponent(skillComponentType);

                // 通过反射写入基础字段（skillName / energyCost / isUnlocked）
                SetSkillBaseFields(skillComp, skillType);

                // 保存预制体
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                UnityEngine.Object.DestroyImmediate(go);

                if (prefab == null)
                {
                    Debug.LogError($"{LOG_TAG}   [Skill] ✗ 预制体保存失败：{prefabPath}");
                    continue;
                }

                Debug.Log($"{LOG_TAG}   [Skill] ✦ 已保存：{prefabPath}");

                prefabProp.objectReferenceValue = prefab;
                anyDirty = true;
            }

            if (anyDirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"{LOG_TAG}   [Skill] ✓ 已将技能预制体引用写入 {dataAssetPath}");
            }

            return anyDirty;
        }

        /// <summary>
        /// 在所有已加载的程序集中查找继承自 <see cref="Skill"/> 的指定类型。
        /// </summary>
        /// <param name="className">目标类名（不含命名空间）</param>
        /// <returns>找到的 Type，未找到时返回 null</returns>
        private static Type FindSkillType(string className)
        {
            Type baseType = typeof(Skill);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in assembly.GetTypes())
                {
                    if (t.Name == className && baseType.IsAssignableFrom(t) && !t.IsAbstract)
                        return t;
                }
            }

            return null;
        }

        /// <summary>
        /// 通过反射向 Skill 组件写入基础字段（skillName / energyCost / isUnlocked）。
        /// 字段名与 <see cref="Skill"/> 基类中的 SerializeField 名称保持一致。
        /// </summary>
        private static void SetSkillBaseFields(Component skillComp, SkillType skillType)
        {
            // 默认能量消耗表（与现有 SkillPrefabGenerator 保持一致）
            var defaultEnergyCosts = new Dictionary<SkillType, int>
            {
                { SkillType.TimeRewind, 1 },
                { SkillType.IceFreeze,  2 },
                { SkillType.BurstPush,  2 },
                { SkillType.PhaseShift, 2 },
                { SkillType.EchoClone,  3 },
                { SkillType.GravityWell,3 },
                { SkillType.TimeFreeze, 2 },
            };

            // 默认技能显示名称表
            var defaultSkillNames = new Dictionary<SkillType, string>
            {
                { SkillType.TimeRewind, "时间回溯" },
                { SkillType.IceFreeze,  "冰封固化" },
                { SkillType.BurstPush,  "爆裂推进" },
                { SkillType.PhaseShift, "相位转移" },
                { SkillType.EchoClone,  "回响分身" },
                { SkillType.GravityWell,"重力井"   },
                { SkillType.TimeFreeze, "时间冻结" },
            };

            var bindingFlags =
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance  |
                System.Reflection.BindingFlags.FlattenHierarchy;

            Type type = skillComp.GetType();

            TrySetField(type, skillComp, "skillName",
                defaultSkillNames.TryGetValue(skillType, out string name) ? name : skillType.ToString(),
                bindingFlags);

            TrySetField(type, skillComp, "energyCost",
                defaultEnergyCosts.TryGetValue(skillType, out int cost) ? cost : 1,
                bindingFlags);

            TrySetField(type, skillComp, "isUnlocked", true, bindingFlags);
        }

        /// <summary>
        /// 安全地通过反射设置字段值，字段不存在时输出警告而非抛出异常。
        /// </summary>
        private static void TrySetField(
            Type type,
            object target,
            string fieldName,
            object value,
            System.Reflection.BindingFlags flags)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"{LOG_TAG}     ⚠ 字段 '{fieldName}' 在类型 '{type.Name}' 中未找到，已跳过赋值。");
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// 确保 <paramref name="parent"/>/<paramref name="child"/> 目录存在，不存在则创建。
        /// </summary>
        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = Path.Combine(parent, child).Replace('\\', '/');

            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
                Debug.Log($"{LOG_TAG} 已创建目录：{fullPath}");
            }
        }

        #endregion
    }
}
