using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 场景层信息
    /// 记录每一层场景的关键信息，用于递归回退时恢复状态
    /// </summary>
    [Serializable]
    public class SceneLayer
    {
        /// <summary>
        /// 场景名称（用于加载/卸载）
        /// </summary>
        public string sceneName;
        
        /// <summary>
        /// 玩家进入此层时的位置（用于返回时恢复）
        /// </summary>
        public Vector3 playerEntryPosition;
        
        /// <summary>
        /// 父层级中传送门箱子的实例 ID（用于返回时定位到正确的箱子旁边）
        /// </summary>
        public int parentPortalBoxId;
        
        /// <summary>
        /// 相机状态（用于返回时恢复相机）
        /// </summary>
        public CameraState cameraState;
        
        /// <summary>
        /// 是否是最外层场景
        /// </summary>
        public bool isOutermost;
        
        /// <summary>
        /// 进入时间戳（可选，用于调试）
        /// </summary>
        public float timestamp;
    }
    
    /// <summary>
    /// [Echoes Mod]: 相机状态
    /// </summary>
    [Serializable]
    public struct CameraState
    {
        public Vector3 position;
        public float orthographicSize;
        public bool isValid;
        
        public CameraState(Camera camera)
        {
            if (camera != null)
            {
                position = camera.transform.position;
                orthographicSize = camera.orthographicSize;
                isValid = true;
            }
            else
            {
                position = Vector3.zero;
                orthographicSize = 5f;
                isValid = false;
            }
        }
        
        public void ApplyTo(Camera camera)
        {
            if (!isValid || camera == null) return;
            camera.transform.position = position;
            camera.orthographicSize = orthographicSize;
        }
    }

    /// <summary>
    /// [Echoes Mod]: 场景栈管理器
    /// 管理递归嵌套场景的加载、卸载和状态恢复
    /// 
    /// 单例保护机制：
    /// - DontDestroyOnLoad 确保 GameObject 跨场景存活
    /// - _layerStack 作为实例字段随单例一起存活
    /// - 不存储 Scene 引用，只存储场景名和元数据
    /// </summary>
    public class SceneStackManager : MonoBehaviour
    {
        #region Singleton
        
        private static SceneStackManager _instance;
        public static SceneStackManager Instance => _instance;
        
        #endregion
        
        #region Fields
        
        /// <summary>
        /// 场景层栈，记录嵌套层级
        /// </summary>
        private Stack<SceneLayer> _layerStack;
        
        /// <summary>
        /// 当前是否正在加载场景
        /// </summary>
        private bool _isLoading;
        
        /// <summary>
        /// 当前是否正在卸载场景
        /// </summary>
        private bool _isUnloading;
        
        /// <summary>
        /// 跨场景复用的 Player 对象
        /// </summary>
        private GameObject _sharedPlayer;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// 当场景层被压入栈时触发
        /// </summary>
        public event Action<SceneLayer> OnLayerPushed;
        
        /// <summary>
        /// 当场景层被弹出栈时触发
        /// </summary>
        public event Action<SceneLayer> OnLayerPopped;
        
        /// <summary>
        /// 当场景加载完成时触发
        /// </summary>
        public event Action<string> OnSceneLoadComplete;
        
        /// <summary>
        /// 当场景卸载完成时触发
        /// </summary>
        public event Action<string> OnSceneUnloadComplete;
        
        /// <summary>
        /// [Echoes Mod]: 当内层场景完成时触发（跨层级通信）
        /// 参数：(portalBoxId, isCompleted)
        /// </summary>
        public static event Action<int, bool> OnInnerSceneCompleted;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前场景栈深度
        /// </summary>
        public int StackDepth => _layerStack?.Count ?? 0;
        
        /// <summary>
        /// 是否有内层场景
        /// </summary>
        public bool HasInnerScene => StackDepth > 1;
        
        /// <summary>
        /// 是否正在处理场景
        /// </summary>
        public bool IsProcessing => _isLoading || _isUnloading;
        
        /// <summary>
        /// 获取当前层信息
        /// </summary>
        public SceneLayer CurrentLayer => _layerStack?.Count > 0 ? _layerStack.Peek() : null;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureSingleton();
            _layerStack = new Stack<SceneLayer>();
        }
        
        private void Start()
        {
            // 将初始场景压入栈
            Scene baseScene = SceneManager.GetActiveScene();
            PushLayer(new SceneLayer
            {
                sceneName = baseScene.name,
                playerEntryPosition = Vector3.zero,
                parentPortalBoxId = -1,
                cameraState = new CameraState(Camera.main),
                isOutermost = true,
                timestamp = Time.time
            });
            
            Debug.Log($"[SceneStackManager] Base scene '{baseScene.name}' pushed to stack.");
        }
        
        #endregion
        
        #region Public Methods - Layer Management
        
        /// <summary>
        /// [Echoes Mod]: 压入新的场景层
        /// </summary>
        public void PushLayer(SceneLayer layer)
        {
            _layerStack.Push(layer);
            OnLayerPushed?.Invoke(layer);
            Debug.Log($"[SceneStackManager] Layer pushed: {layer.sceneName}. Stack depth: {StackDepth}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 弹出当前场景层
        /// </summary>
        public SceneLayer PopLayer()
        {
            if (_layerStack == null || _layerStack.Count <= 1)
            {
                Debug.LogWarning("[SceneStackManager] Cannot pop base layer.");
                return null;
            }
            
            SceneLayer layer = _layerStack.Pop();
            OnLayerPopped?.Invoke(layer);
            Debug.Log($"[SceneStackManager] Layer popped: {layer.sceneName}. Stack depth: {StackDepth}");
            return layer;
        }
        
        /// <summary>
        /// 获取父层信息
        /// </summary>
        public SceneLayer GetParentLayer()
        {
            if (_layerStack == null || _layerStack.Count < 2)
                return null;
            
            SceneLayer[] layers = _layerStack.ToArray();
            return layers[1]; // 栈中第二个是父层
        }
        
        /// <summary>
        /// [Echoes Mod]: 获取完整的场景层栈（用于 UI 显示）
        /// </summary>
        public List<SceneLayer> GetAllLayers()
        {
            if (_layerStack == null || _layerStack.Count == 0)
                return new List<SceneLayer>();
            
            // 栈顶是当前层，需要反转顺序（从外层到内层）
            SceneLayer[] layers = _layerStack.ToArray();
            var result = new List<SceneLayer>(layers.Length);
            
            // 反转：从最外层到当前层
            for (int i = layers.Length - 1; i >= 0; i--)
            {
                result.Add(layers[i]);
            }
            
            return result;
        }
        
        #endregion
        
        #region Public Methods - Scene Transition
        
        /// <summary>
        /// [Echoes Mod]: 进入内层场景（通过 PortalBox）
        /// </summary>
        public void EnterInnerScene(string innerSceneName, Echoes.Puzzles.PortalBox portalBox)
        {
            if (string.IsNullOrEmpty(innerSceneName))
            {
                Debug.LogError("[SceneStackManager] Inner scene name is null or empty.");
                return;
            }
            
            if (_isLoading)
            {
                Debug.LogWarning("[SceneStackManager] Already loading a scene.");
                return;
            }
            
            StartCoroutine(LoadInnerSceneCoroutine(innerSceneName, portalBox));
        }
        
        /// <summary>
        /// [Echoes Mod]: 返回父层场景
        /// </summary>
        public void ReturnToParentScene()
        {
            if (!HasInnerScene)
            {
                Debug.LogWarning("[SceneStackManager] No inner scene to return from.");
                return;
            }
            
            if (_isUnloading)
            {
                Debug.LogWarning("[SceneStackManager] Already unloading a scene.");
                return;
            }
            
            StartCoroutine(UnloadInnerSceneCoroutine());
        }
        
        /// <summary>
        /// [Echoes Mod]: 返回外层场景（兼容旧 API）
        /// </summary>
        public void ReturnToOuterScene() => ReturnToParentScene();
        
        /// <summary>
        /// [Echoes Mod]: 通知父层传送门完成状态（跨层级通信）
        /// 由内层场景调用，通知父层对应的 PortalBox 改变状态
        /// </summary>
        /// <param name="portalBoxId">父层传送门的实例 ID</param>
        /// <param name="completed">是否完成</param>
        public void NotifyPortalCompleted(int portalBoxId, bool completed)
        {
            Debug.Log($"[SceneStackManager] Notifying portal {portalBoxId} completion: {completed}");
            OnInnerSceneCompleted?.Invoke(portalBoxId, completed);
        }
        
        #endregion
        
        #region Private Methods - Scene Loading
        
        private IEnumerator LoadInnerSceneCoroutine(string sceneName, Echoes.Puzzles.PortalBox portalBox)
        {
            _isLoading = true;
            
            Debug.Log($"[SceneStackManager] Loading inner scene: {sceneName}");
            
            // 1. 保存当前层的状态
            SceneLayer currentLayer = CurrentLayer;
            Camera mainCamera = Camera.main;
            
            // 2. 找到并缓存 Player
            _sharedPlayer = GameObject.FindGameObjectWithTag("Player");
            Vector3 playerPositionBeforeEnter = Vector3.zero;
            int portalBoxId = -1;
            
            if (_sharedPlayer != null)
            {
                playerPositionBeforeEnter = _sharedPlayer.transform.position;
                DontDestroyOnLoad(_sharedPlayer);
            }
            
            if (portalBox != null)
            {
                portalBoxId = portalBox.GetInstanceID();
            }
            
            // 3. 保存相机状态
            CameraState cameraState = new CameraState(mainCamera);
            
            // 4. 暂停当前场景的输入
            SuspendCurrentScene();
            
            // 5. 异步加载内层场景
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            if (loadOperation == null)
            {
                Debug.LogError($"[SceneStackManager] Failed to load scene: {sceneName}");
                _isLoading = false;
                ResumeCurrentScene();
                yield break;
            }
            
            loadOperation.allowSceneActivation = true;
            
            while (!loadOperation.isDone)
            {
                yield return null;
            }
            
            // 6. 获取加载的场景
            Scene innerScene = SceneManager.GetSceneByName(sceneName);
            
            if (!innerScene.isLoaded)
            {
                Debug.LogError($"[SceneStackManager] Scene loaded but not valid: {sceneName}");
                _isLoading = false;
                ResumeCurrentScene();
                yield break;
            }
            
            // 7. 设置内层场景为活动场景
            SceneManager.SetActiveScene(innerScene);
            
            // 8. 等待一帧让 InnerSceneLevelLoader 完成初始化
            yield return null;
            
            // 9. 计算场景偏移量（在 PushLayer 之前计算，使用当前深度 + 1）
            int newDepth = StackDepth + 1;
            Vector3 sceneOffset = new Vector3(newDepth * 1000f, 0, 0);
            
            // 10. 压入新的场景层（记录进入前的状态）
            SceneLayer newLayer = new SceneLayer
            {
                sceneName = sceneName,
                playerEntryPosition = playerPositionBeforeEnter,
                parentPortalBoxId = portalBoxId,
                cameraState = cameraState,
                isOutermost = false,
                timestamp = Time.time
            };
            PushLayer(newLayer);
            
            // 11. [Echoes Mod]: 应用世界坐标偏移，实现场景隔离
            ApplySceneOffset(innerScene, sceneOffset);
            Debug.Log($"[SceneStackManager] Applied scene offset: {sceneOffset} for depth {newDepth}");
            
            // 11. 将 Player 移动到内层场景的出生点（加上偏移）
            if (_sharedPlayer != null)
            {
                SceneManager.MoveGameObjectToScene(_sharedPlayer, innerScene);
                
                InnerSceneLevelLoader innerLoader = FindInnerSceneLevelLoader(innerScene);
                if (innerLoader != null)
                {
                    // 出生点也需要加上场景偏移
                    Vector3 spawnPos = innerLoader.PlayerSpawnPosition + sceneOffset;
                    _sharedPlayer.transform.position = spawnPos;
                    Debug.Log($"[SceneStackManager] Player moved to inner spawn: {spawnPos}");
                }
                
                RefreshPlayerMovementController(innerScene, _sharedPlayer);
            }
            
            // 11.5 [Echoes Mod]: 设置内层场景完成器的父传送门 ID
            SetupInnerSceneCompleters(innerScene, portalBoxId);
            
            // 12. 激活内层场景的输入
            ActivateScene(innerScene);
            
            // 13. 调整相机（相机位置也需要加上偏移）
            AdjustCameraForInnerScene(innerScene, sceneOffset);
            
            _isLoading = false;
            OnSceneLoadComplete?.Invoke(sceneName);
            
            Debug.Log($"[SceneStackManager] Inner scene '{sceneName}' loaded. Stack depth: {StackDepth}");
        }
        
        private IEnumerator UnloadInnerSceneCoroutine()
        {
            _isUnloading = true;
            
            // 1. 弹出当前层
            SceneLayer poppedLayer = PopLayer();
            if (poppedLayer == null)
            {
                _isUnloading = false;
                yield break;
            }
            
            string sceneName = poppedLayer.sceneName;
            Debug.Log($"[SceneStackManager] Unloading inner scene: {sceneName}");
            
            // 2. 获取父层信息
            SceneLayer parentLayer = CurrentLayer;
            if (parentLayer == null)
            {
                Debug.LogError("[SceneStackManager] No parent layer found.");
                _isUnloading = false;
                yield break;
            }
            
            // 3. 暂停当前场景
            Scene currentScene = SceneManager.GetSceneByName(sceneName);
            SuspendScene(currentScene);
            
            // 4. 将 Player 设为 DontDestroyOnLoad
            if (_sharedPlayer != null)
            {
                DontDestroyOnLoad(_sharedPlayer);
            }
            
            // 5. 异步卸载内层场景
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneName);
            
            if (unloadOperation == null)
            {
                Debug.LogError($"[SceneStackManager] Failed to unload scene: {sceneName}");
                _isUnloading = false;
                yield break;
            }
            
            while (!unloadOperation.isDone)
            {
                yield return null;
            }
            
            yield return Resources.UnloadUnusedAssets();
            
            // 6. 获取父层场景并设为活动场景
            Scene parentScene = SceneManager.GetSceneByName(parentLayer.sceneName);
            if (parentScene.isLoaded)
            {
                SceneManager.SetActiveScene(parentScene);
            }
            
            // 7. 将 Player 移入父层场景并恢复位置
            if (_sharedPlayer != null)
            {
                SceneManager.MoveGameObjectToScene(_sharedPlayer, parentScene);
                
                // 恢复到进入前的位置（传送门箱子旁边）
                _sharedPlayer.transform.position = poppedLayer.playerEntryPosition;
                Debug.Log($"[SceneStackManager] Player returned to position: {poppedLayer.playerEntryPosition}");
                
                RefreshPlayerMovementController(parentScene, _sharedPlayer);
            }
            
            // 8. 恢复父层场景的输入
            ActivateScene(parentScene);
            
            // 9. 恢复相机状态
            Camera mainCamera = Camera.main;
            if (mainCamera != null && poppedLayer.cameraState.isValid)
            {
                poppedLayer.cameraState.ApplyTo(mainCamera);
                Debug.Log("[SceneStackManager] Camera state restored");
            }
            
            _isUnloading = false;
            OnSceneUnloadComplete?.Invoke(sceneName);
            
            Debug.Log($"[SceneStackManager] Returned to parent scene '{parentLayer.sceneName}'. Stack depth: {StackDepth}");
        }
        
        #endregion
        
        #region Private Methods - Helpers
        
        private InnerSceneLevelLoader FindInnerSceneLevelLoader(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var loader = root.GetComponentInChildren<InnerSceneLevelLoader>(true);
                if (loader != null) return loader;
            }
            return null;
        }
        
        /// <summary>
        /// [Echoes Mod]: 设置内层场景完成器的父传送门 ID
        /// </summary>
        private void SetupInnerSceneCompleters(Scene scene, int parentPortalId)
        {
            if (parentPortalId <= 0) return;
            
            foreach (var root in scene.GetRootGameObjects())
            {
                var completers = root.GetComponentsInChildren<Echoes.Puzzles.InnerSceneCompleter>(true);
                foreach (var completer in completers)
                {
                    completer.SetParentPortalId(parentPortalId);
                    Debug.Log($"[SceneStackManager] Set parent portal ID {parentPortalId} for completer: {completer.name}");
                }
            }
        }
        
        private void RefreshPlayerMovementController(Scene scene, GameObject player)
        {
            var movable = player.GetComponent<Movable>();
            if (movable == null) return;
            
            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<PlayerMovementController>(true);
                if (controller != null)
                {
                    controller.SetPlayer(movable);
                    Debug.Log($"[SceneStackManager] PlayerMovementController refreshed in scene: {scene.name}");
                    return;
                }
            }
            
            if (GameStateRecorder.Instance != null)
            {
                GameStateRecorder.Instance.SetPlayer(movable);
                GameStateRecorder.Instance.Clear();
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 应用场景偏移，实现递归场景的空间隔离
        /// </summary>
        private void ApplySceneOffset(Scene scene, Vector3 offset)
        {
            if (!scene.isLoaded) return;
            
            GameObject[] rootObjects = scene.GetRootGameObjects();
            int offsetCount = 0;
            
            foreach (var obj in rootObjects)
            {
                obj.transform.position += offset;
                offsetCount++;
            }
            
            Debug.Log($"[SceneStackManager] Applied offset {offset} to {offsetCount} root objects in scene: {scene.name}");
        }
        
        private void AdjustCameraForInnerScene(Scene innerScene, Vector3 sceneOffset)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            InnerSceneLevelLoader loader = FindInnerSceneLevelLoader(innerScene);
            if (loader == null) return;
            
            Bounds bounds = loader.LevelBounds;
            if (bounds.size.sqrMagnitude <= 0) return;
            
            bounds.Expand(1);
            float verticalSize = bounds.size.y / 2f;
            float horizontalSize = bounds.size.x * mainCamera.pixelHeight / mainCamera.pixelWidth / 2f;
            
            // 相机位置需要加上场景偏移
            Vector3 targetPos = new Vector3(bounds.center.x, bounds.center.y, -10f) + sceneOffset;
            mainCamera.transform.position = targetPos;
            mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize, 5f);
            
            Debug.Log($"[SceneStackManager] Camera adjusted to: {targetPos}");
        }
        
        #endregion
        
        #region Private Methods - Scene State Control
        
        private void SuspendCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SuspendScene(currentScene);
        }
        
        private void ResumeCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            ActivateScene(currentScene);
        }
        
        private void SuspendScene(Scene scene)
        {
            if (!scene.isLoaded) return;
            
            foreach (var obj in scene.GetRootGameObjects())
            {
                var movementController = obj.GetComponentInChildren<PlayerMovementController>(true);
                if (movementController != null) movementController.enabled = false;
                
                var gameController = obj.GetComponentInChildren<GameController>(true);
                if (gameController != null) gameController.enabled = false;
                
                var echoesInput = obj.GetComponentInChildren<EchoesInputController>(true);
                if (echoesInput != null) echoesInput.enabled = false;
            }
        }
        
        private void ActivateScene(Scene scene)
        {
            if (!scene.isLoaded) return;
            
            foreach (var obj in scene.GetRootGameObjects())
            {
                var movementController = obj.GetComponentInChildren<PlayerMovementController>(true);
                if (movementController != null) movementController.enabled = true;
                
                var gameController = obj.GetComponentInChildren<GameController>(true);
                if (gameController != null) gameController.enabled = true;
                
                var echoesInput = obj.GetComponentInChildren<EchoesInputController>(true);
                if (echoesInput != null) echoesInput.enabled = true;
            }
        }
        
        #endregion
        
        #region Private Methods - Singleton
        
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
        
        #endregion
    }
}
