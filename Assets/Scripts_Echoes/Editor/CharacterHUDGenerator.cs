using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Echoes.UI;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: 角色 HUD 生成器
    /// 菜单 -> Echoes -> Generate Character HUD
    /// 
    /// 生成规则：
    /// - 挂在 LevelScene 的 Canvas 下
    /// - 锚点：左下角
    /// - 暂停时自动隐藏
    /// </summary>
    public class CharacterHUDGenerator : EditorWindow
    {
        [MenuItem("Echoes/Generate Character HUD")]
        public static void GenerateHUD()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "场景中没有找到 Canvas。\n请先确认已打开 LevelScene。", "确定");
                return;
            }

            // 如果已存在则先删除
            Transform existing = canvas.transform.Find("CharacterHUD");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                Debug.Log("[CharacterHUDGenerator] Removed existing HUD.");
            }

            // ── 根节点 ────────────────────────────────────────
            GameObject root = new GameObject("CharacterHUD");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0, 0);
            rootRect.anchorMax = new Vector2(0, 0);
            rootRect.pivot     = new Vector2(0, 0);
            rootRect.anchoredPosition = new Vector2(16, 16);
            rootRect.sizeDelta = new Vector2(230, 115);

            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.05f, 0.05f, 0.1f, 0.72f);

            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // ── 头像边框 ──────────────────────────────────────
            GameObject borderGO = CreateRect("PortraitBorder", root.transform,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(76, 76));
            Image borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(0.4f, 0.7f, 1f);

            // ── 头像 ──────────────────────────────────────────
            GameObject portraitGO = CreateRect("Portrait", borderGO.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            RectTransform pRect = portraitGO.GetComponent<RectTransform>();
            pRect.offsetMin = new Vector2(3, 3);
            pRect.offsetMax = new Vector2(-3, -3);
            Image portraitImg = portraitGO.AddComponent<Image>();
            portraitImg.color = new Vector4(0.4f, 0.7f, 1f, 1f);

            // ── 右侧信息パネル ────────────────────────────────
            GameObject infoGO = CreateRect("InfoPanel", root.transform,
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f),
                new Vector2(96, 0), Vector2.zero);
            RectTransform infoRect = infoGO.GetComponent<RectTransform>();
            infoRect.offsetMin = new Vector2(96, 8);
            infoRect.offsetMax = new Vector2(-8, -8);

            // 角色名
            GameObject nameGO = CreateRect("CharacterName", infoGO.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                Vector2.zero, new Vector2(0, 22));
            TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = "星野未来";
            nameText.fontSize = 15;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // 技能名
            GameObject skillNameGO = CreateRect("SkillName", infoGO.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, -24), new Vector2(0, 18));
            TextMeshProUGUI skillNameText = skillNameGO.AddComponent<TextMeshProUGUI>();
            skillNameText.text = "[Q] 时间回溯";
            skillNameText.fontSize = 12;
            skillNameText.color = new Color(0.7f, 0.9f, 1f);

            // 技能描述
            GameObject skillDescGO = CreateRect("SkillDesc", infoGO.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, -44), new Vector2(0, 16));
            TextMeshProUGUI skillDescText = skillDescGO.AddComponent<TextMeshProUGUI>();
            skillDescText.text = "撤销最近3步操作";
            skillDescText.fontSize = 10;
            skillDescText.color = new Color(0.6f, 0.6f, 0.6f);

            // ── 能量点 ────────────────────────────────────────
            GameObject energyGO = CreateRect("EnergyDots", infoGO.transform,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(0, 4), new Vector2(72, 14));

            Image[] energyDots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject dotGO = CreateRect($"Dot_{i}", energyGO.transform,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(i * 22, 0), new Vector2(16, 16));
                Image dotImg = dotGO.AddComponent<Image>();
                dotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                energyDots[i] = dotImg;
            }

            // ── Tab 切换提示 ──────────────────────────────────
            GameObject hintGO = CreateRect("SwitchHint", root.transform,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1),
                new Vector2(8, 2), new Vector2(-16, 13));
            TextMeshProUGUI hintText = hintGO.AddComponent<TextMeshProUGUI>();
            hintText.text = "[Tab] 切换角色";
            hintText.fontSize = 9;
            hintText.color = new Color(0.55f, 0.55f, 0.55f);

            // ── 挂载 Controller ───────────────────────────────
            CharacterHUDController ctrl = root.AddComponent<CharacterHUDController>();
            SerializedObject so = new SerializedObject(ctrl);

            so.FindProperty("portraitImage").objectReferenceValue     = portraitImg;
            so.FindProperty("portraitBorder").objectReferenceValue    = borderImg;
            so.FindProperty("characterNameText").objectReferenceValue = nameText;
            so.FindProperty("skillNameText").objectReferenceValue     = skillNameText;
            so.FindProperty("skillDescText").objectReferenceValue     = skillDescText;
            so.FindProperty("switchHintText").objectReferenceValue    = hintText;
            so.FindProperty("hudCanvasGroup").objectReferenceValue    = cg;
            so.FindProperty("hideOnPause").boolValue                  = true;

            SerializedProperty dotsArr = so.FindProperty("energyDots");
            dotsArr.arraySize = 3;
            for (int i = 0; i < 3; i++)
                dotsArr.GetArrayElementAtIndex(i).objectReferenceValue = energyDots[i];

            so.ApplyModifiedProperties();

            // ── 保存预制体 ────────────────────────────────────
            string prefabPath = "Assets/Resources/UI/CharacterHUD.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);
            AssetDatabase.Refresh();

            Debug.Log($"[CharacterHUDGenerator] HUD created at {prefabPath}");
            EditorUtility.DisplayDialog("HUD Generated",
                "角色 HUD 已创建！\n\n位置：左下角\n暂停时自动隐藏\n\n请运行游戏测试。", "确定");
        }

        // ── 辅助方法 ──────────────────────────────────────────
        private static GameObject CreateRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            return go;
        }
    }
}
