using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Echoes.UI;

namespace Echoes.Editor
{
    /// <summary>
    /// [Echoes Mod]: 关卡信息 HUD 生成器
    /// 菜单 -> Echoes -> Generate Level Info HUD
    /// 在场景 Canvas 顶部居中生成关卡名称 + 步数显示
    /// </summary>
    public class LevelInfoHUDGenerator : EditorWindow
    {
        [MenuItem("Echoes/Generate Level Info HUD")]
        public static void Generate()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "场景中没有找到 Canvas，请先确认已打开 LevelScene。", "确定");
                return;
            }

            // 已存在则先删除
            var existing = canvas.transform.Find("LevelInfoHUD");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
                Debug.Log("[LevelInfoHUDGenerator] Removed existing LevelInfoHUD.");
            }

            // ── 根节点（顶部居中）────────────────────────────
            GameObject root = new GameObject("LevelInfoHUD");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin        = new Vector2(0.5f, 1f);
            rootRect.anchorMax        = new Vector2(0.5f, 1f);
            rootRect.pivot            = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -12f);
            rootRect.sizeDelta        = new Vector2(300f, 52f);

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.65f);

            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // ── 关卡名称（上行）──────────────────────────────
            GameObject nameGO = CreateRect("LevelName", root.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -4f), new Vector2(-16f, 22f));

            TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text      = "心域序章";
            nameText.fontSize  = 14;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color     = Color.white;
            nameText.alignment = TextAlignmentOptions.Center;

            // ── 步数（下行）──────────────────────────────────
            GameObject stepGO = CreateRect("StepCount", root.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(-16f, 20f));

            TextMeshProUGUI stepText = stepGO.AddComponent<TextMeshProUGUI>();
            stepText.text      = "步数：0";
            stepText.fontSize  = 12;
            stepText.color     = new Color(0.8f, 0.9f, 1f);
            stepText.alignment = TextAlignmentOptions.Center;

            // ── 挂载 Controller ───────────────────────────────
            LevelInfoHUDController ctrl = root.AddComponent<LevelInfoHUDController>();
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("levelNameText").objectReferenceValue = nameText;
            so.FindProperty("stepCountText").objectReferenceValue = stepText;
            so.FindProperty("hudCanvasGroup").objectReferenceValue = cg;
            so.FindProperty("hideOnPause").boolValue = true;
            so.ApplyModifiedProperties();

            // ── 保存预制体 ────────────────────────────────────
            string prefabPath = "Assets/Resources/UI/LevelInfoHUD.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                root, prefabPath, InteractionMode.UserAction);
            AssetDatabase.Refresh();

            Debug.Log($"[LevelInfoHUDGenerator] Created at {prefabPath}");
            EditorUtility.DisplayDialog("LevelInfoHUD Generated",
                "关卡信息 HUD 已创建！\n\n位置：顶部居中\n内容：关卡名称 + 步数\n\n" +
                "注意：关卡名称需要先运行\n「Echoes → Generate Level Data Assets」\n才能正确显示。",
                "确定");
        }

        private static GameObject CreateRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
            return go;
        }
    }
}
