using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Core;

namespace Echoes.UI
{
    /// <summary>
    /// [Echoes Mod]: 递归深度指示器
    /// 显示当前场景路径，支持紧急跳出功能
    /// </summary>
    public class DepthIndicator : MonoBehaviour
    {
        #region Settings
        
        [Header("Display Settings")]
        [SerializeField] private bool showIndicator = true;
        [SerializeField] private Vector2 position = new Vector2(20, 20);  // 左上角
        [SerializeField] private int fontSize = 16;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color separatorColor = new Color(0.7f, 0.7f, 0.7f);
        
        [Header("Scene Names (可选，用于显示友好名称)")]
        [SerializeField] private List<SceneDisplayName> sceneDisplayNames = new List<SceneDisplayName>();
        
        [Header("Emergency Exit Settings")]
        [SerializeField] private float emergencyExitHoldTime = 2f;  // 长按时间
        [SerializeField] private bool showEmergencyExitHint = true;
        
        #endregion
        
        #region Fields
        
        private GUIStyle _pathStyle;
        private GUIStyle _separatorStyle;
        private GUIStyle _hintStyle;
        private float _escHoldTime = 0f;
        private bool _isHoldingEsc = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            if (SceneStackManager.Instance != null)
            {
                SceneStackManager.Instance.OnLayerPushed += OnLayerChanged;
                SceneStackManager.Instance.OnLayerPopped += OnLayerChanged;
            }
        }
        
        private void OnDisable()
        {
            if (SceneStackManager.Instance != null)
            {
                SceneStackManager.Instance.OnLayerPushed -= OnLayerChanged;
                SceneStackManager.Instance.OnLayerPopped -= OnLayerChanged;
            }
        }
        
        private void Update()
        {
            HandleEmergencyExit();
        }
        
        private void OnGUI()
        {
            if (!showIndicator) return;
            
            InitializeStyles();
            DrawPath();
            DrawEmergencyExitHint();
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeStyles()
        {
            if (_pathStyle == null)
            {
                _pathStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    normal = { textColor = textColor },
                    alignment = TextAnchor.MiddleLeft
                };
            }
            
            if (_separatorStyle == null)
            {
                _separatorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    normal = { textColor = separatorColor },
                    alignment = TextAnchor.MiddleLeft
                };
            }
            
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize - 2,
                    normal = { textColor = new Color(1f, 0.5f, 0.5f) },
                    alignment = TextAnchor.MiddleLeft
                };
            }
        }
        
        private void DrawPath()
        {
            if (SceneStackManager.Instance == null) return;
            
            // 获取场景栈
            var layers = GetSceneLayers();
            if (layers == null || layers.Count == 0) return;
            
            // 构建路径字符串
            float x = position.x;
            float y = position.y;
            
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                string displayName = GetDisplayName(layers[i].sceneName);
                
                // 绘制场景名
                GUIContent content = new GUIContent(displayName);
                Vector2 size = _pathStyle.CalcSize(content);
                Rect rect = new Rect(x, y, size.x, size.y);
                GUI.Label(rect, displayName, _pathStyle);
                x += size.x + 5;
                
                // 绘制分隔符（除了最后一个）
                if (i > 0)
                {
                    GUIContent sepContent = new GUIContent(">");
                    Vector2 sepSize = _separatorStyle.CalcSize(sepContent);
                    Rect sepRect = new Rect(x, y, sepSize.x, sepSize.y);
                    GUI.Label(sepRect, ">", _separatorStyle);
                    x += sepSize.x + 5;
                }
            }
        }
        
        private void DrawEmergencyExitHint()
        {
            if (!showEmergencyExitHint) return;
            if (SceneStackManager.Instance == null || !SceneStackManager.Instance.HasInnerScene) return;
            
            float y = position.y + fontSize + 10;
            
            if (_isHoldingEsc)
            {
                // 显示进度条
                float progress = _escHoldTime / emergencyExitHoldTime;
                string progressText = $"紧急跳出中... {Mathf.RoundToInt(progress * 100)}%";
                
                GUIContent content = new GUIContent(progressText);
                Vector2 size = _hintStyle.CalcSize(content);
                Rect rect = new Rect(position.x, y, size.x, size.y);
                GUI.Label(rect, progressText, _hintStyle);
                
                // 绘制进度条背景
                float barWidth = 150f;
                float barHeight = 4f;
                Rect bgRect = new Rect(position.x, y + size.y + 2, barWidth, barHeight);
                GUI.Box(bgRect, "");
                
                // 绘制进度条前景
                Rect fgRect = new Rect(position.x, y + size.y + 2, barWidth * progress, barHeight);
                GUI.DrawTexture(fgRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.red, 0, 0);
            }
            else
            {
                string hintText = "长按 Esc 紧急跳出";
                GUIContent content = new GUIContent(hintText);
                Vector2 size = _hintStyle.CalcSize(content);
                Rect rect = new Rect(position.x, y, size.x, size.y);
                GUI.Label(rect, hintText, _hintStyle);
            }
        }
        
        private void HandleEmergencyExit()
        {
            if (SceneStackManager.Instance == null || !SceneStackManager.Instance.HasInnerScene) return;
            
            // 检测 Esc 键
            if (Keyboard.current != null && Keyboard.current.escapeKey.isPressed)
            {
                if (!_isHoldingEsc)
                {
                    _isHoldingEsc = true;
                    _escHoldTime = 0f;
                }
                else
                {
                    _escHoldTime += Time.deltaTime;
                    
                    // 达到长按时间，执行紧急跳出
                    if (_escHoldTime >= emergencyExitHoldTime)
                    {
                        ExecuteEmergencyExit();
                        _escHoldTime = 0f;
                        _isHoldingEsc = false;
                    }
                }
            }
            else
            {
                _isHoldingEsc = false;
                _escHoldTime = 0f;
            }
        }
        
        private void ExecuteEmergencyExit()
        {
            if (SceneStackManager.Instance == null) return;
            
            Debug.Log("[DepthIndicator] Emergency exit triggered!");
            StartCoroutine(PopAllLayersCoroutine());
        }
        
        private IEnumerator PopAllLayersCoroutine()
        {
            // 逐层弹出，直到只剩最外层
            while (SceneStackManager.Instance != null && SceneStackManager.Instance.HasInnerScene)
            {
                SceneStackManager.Instance.ReturnToParentScene();
                
                // 等待当前卸载完成
                while (SceneStackManager.Instance.IsProcessing)
                {
                    yield return null;
                }
                
                // 短暂延迟，避免连续操作
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("[DepthIndicator] Emergency exit complete!");
        }
        
        private List<SceneLayer> GetSceneLayers()
        {
            if (SceneStackManager.Instance == null)
                return new List<SceneLayer>();
            
            return SceneStackManager.Instance.GetAllLayers();
        }
        
        private string GetDisplayName(string sceneName)
        {
            foreach (var mapping in sceneDisplayNames)
            {
                if (mapping.sceneName == sceneName)
                    return mapping.displayName;
            }
            
            // 默认：将场景名转换为友好格式
            // 例如 "InnerLevel_Test" -> "记忆盒子"
            return ConvertSceneNameToFriendly(sceneName);
        }
        
        private string ConvertSceneNameToFriendly(string sceneName)
        {
            // 简单的名称转换逻辑
            if (string.IsNullOrEmpty(sceneName)) return "未知";
            
            // 移除常见前缀
            string name = sceneName.Replace("InnerLevel_", "").Replace("Level", "");
            
            // 替换下划线为空格
            name = name.Replace("_", " ");
            
            return name;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnLayerChanged(SceneLayer layer)
        {
            Debug.Log($"[DepthIndicator] Layer changed: {layer.sceneName}, Depth: {SceneStackManager.Instance?.StackDepth ?? 0}");
        }
        
        #endregion
        
        #region Nested Types
        
        [System.Serializable]
        public struct SceneDisplayName
        {
            public string sceneName;
            public string displayName;
        }
        
        #endregion
    }
}
