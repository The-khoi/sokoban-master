using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 层级可见性管理器
    /// 管理递归嵌套场景的渲染优化：
    /// - 只有当前活跃层和直接父层是 Active 的
    /// - 祖父层及以上通过 SetActive(false) 禁用，但保留在内存中
    /// - 返回父层时自动激活上一级
    /// </summary>
    public class LayerVisibilityManager : MonoBehaviour
    {
        #region Singleton
        
        private static LayerVisibilityManager _instance;
        public static LayerVisibilityManager Instance => _instance;
        
        #endregion
        
        #region Settings
        
        [Header("Visibility Settings")]
        [SerializeField] private bool enableFreezing = true;
        [SerializeField] private int maxActiveLayers = 2;  // 当前层 + 父层
        
        #endregion
        
        #region Fields
        
        /// <summary>
        /// 记录每个场景层中所有 Root GameObject 的引用
        /// Key: 场景名, Value: Root GameObject 列表
        /// </summary>
        private Dictionary<string, List<GameObject>> _layerRootObjects = new Dictionary<string, List<GameObject>>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureSingleton();
        }
        
        private void OnEnable()
        {
            if (SceneStackManager.Instance != null)
            {
                SceneStackManager.Instance.OnLayerPushed += OnLayerPushed;
                SceneStackManager.Instance.OnLayerPopped += OnLayerPopped;
            }
        }
        
        private void OnDisable()
        {
            if (SceneStackManager.Instance != null)
            {
                SceneStackManager.Instance.OnLayerPushed -= OnLayerPushed;
                SceneStackManager.Instance.OnLayerPopped -= OnLayerPopped;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// [Echoes Mod]: 手动刷新层级可见性
        /// </summary>
        public void RefreshVisibility()
        {
            if (!enableFreezing) return;
            if (SceneStackManager.Instance == null) return;
            
            var allLayers = SceneStackManager.Instance.GetAllLayers();
            int currentDepth = allLayers.Count;
            
            // 从最内层向外遍历
            for (int i = 0; i < currentDepth; i++)
            {
                string sceneName = allLayers[i].sceneName;
                bool shouldBeActive = ShouldLayerBeActive(i, currentDepth);
                
                SetLayerActive(sceneName, shouldBeActive);
            }
            
            Debug.Log($"[LayerVisibilityManager] Visibility refreshed. Active layers: {Mathf.Min(maxActiveLayers, currentDepth)}/{currentDepth}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 强制激活指定层
        /// </summary>
        public void ForceActivateLayer(string sceneName)
        {
            SetLayerActive(sceneName, true);
        }
        
        /// <summary>
        /// [Echoes Mod]: 强制冻结指定层
        /// </summary>
        public void ForceFreezeLayer(string sceneName)
        {
            SetLayerActive(sceneName, false);
        }
        
        /// <summary>
        /// [Echoes Mod]: 清除缓存的场景对象引用
        /// </summary>
        public void ClearCache()
        {
            _layerRootObjects.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        private void EnsureSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 判断指定层是否应该激活
        /// </summary>
        private bool ShouldLayerBeActive(int layerIndex, int totalDepth)
        {
            // 最内层（当前层）和其父层应该激活
            // layerIndex 0 = 最外层, layerIndex (totalDepth-1) = 当前层
            int distanceFromCurrent = totalDepth - 1 - layerIndex;
            return distanceFromCurrent < maxActiveLayers;
        }
        
        /// <summary>
        /// 设置场景层的激活状态
        /// </summary>
        private void SetLayerActive(string sceneName, bool active)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                // 场景未加载，从缓存中移除
                _layerRootObjects.Remove(sceneName);
                return;
            }
            
            // 获取或缓存 Root GameObjects
            List<GameObject> rootObjects = GetOrCreateRootObjects(sceneName, scene);
            
            // 设置激活状态
            int changedCount = 0;
            foreach (var obj in rootObjects)
            {
                if (obj != null && obj.activeSelf != active)
                {
                    obj.SetActive(active);
                    changedCount++;
                }
            }
            
            if (changedCount > 0)
            {
                Debug.Log($"[LayerVisibilityManager] Layer '{sceneName}' {(active ? "activated" : "frozen")}. Objects changed: {changedCount}");
            }
        }
        
        /// <summary>
        /// 获取或创建场景的 Root GameObjects 缓存
        /// </summary>
        private List<GameObject> GetOrCreateRootObjects(string sceneName, Scene scene)
        {
            if (!_layerRootObjects.TryGetValue(sceneName, out var rootObjects))
            {
                GameObject[] roots = scene.GetRootGameObjects();
                rootObjects = new List<GameObject>(roots);
                _layerRootObjects[sceneName] = rootObjects;
            }
            
            return rootObjects;
        }
        
        /// <summary>
        /// 更新缓存中的 Root GameObjects
        /// </summary>
        private void UpdateRootObjectsCache(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                _layerRootObjects[sceneName] = new List<GameObject>(roots);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnLayerPushed(SceneLayer layer)
        {
            // 新层压入时，刷新可见性
            UpdateRootObjectsCache(layer.sceneName);
            RefreshVisibility();
        }
        
        private void OnLayerPopped(SceneLayer layer)
        {
            // 层弹出时，从缓存中移除
            _layerRootObjects.Remove(layer.sceneName);
            
            // 刷新剩余层的可见性
            RefreshVisibility();
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug - Print Layer Status")]
        private void DebugPrintLayerStatus()
        {
            if (SceneStackManager.Instance == null)
            {
                Debug.Log("[LayerVisibilityManager] SceneStackManager not found");
                return;
            }
            
            var layers = SceneStackManager.Instance.GetAllLayers();
            Debug.Log($"[LayerVisibilityManager] Total layers: {layers.Count}");
            
            for (int i = 0; i < layers.Count; i++)
            {
                string sceneName = layers[i].sceneName;
                bool shouldBeActive = ShouldLayerBeActive(i, layers.Count);
                bool isCached = _layerRootObjects.ContainsKey(sceneName);
                
                Debug.Log($"  Layer {i}: {sceneName} | Should be active: {shouldBeActive} | Cached: {isCached}");
            }
        }
        
        #endregion
    }
}
