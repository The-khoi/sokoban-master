using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 优化的场景加载器
    /// 实现异步加载、进度显示、对象池化
    /// </summary>
    public class OptimizedSceneLoader : MonoBehaviour
    {
        #region Singleton
        
        private static OptimizedSceneLoader _instance;
        public static OptimizedSceneLoader Instance => _instance;
        
        #endregion
        
        #region Settings
        
        [Header("Loading Settings")]
        [SerializeField] private float minLoadingTime = 0.5f;  // 最小加载时间，避免闪烁
        // [Echoes Mod]: TODO - 待接入 Loading UI 后，在 LoadSceneAsync 中根据此字段控制加载界面显隐
#pragma warning disable CS0414
        [SerializeField] private bool showLoadingScreen = true;
#pragma warning restore CS0414
        
        #endregion
        
        #region Events
        
        public event System.Action<float> OnLoadingProgress;
        public event System.Action OnLoadingStart;
        public event System.Action OnLoadingComplete;
        
        #endregion
        
        #region Properties
        
        public bool IsLoading { get; private set; }
        public float CurrentProgress { get; private set; }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureSingleton();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// [Echoes Mod]: 异步加载场景（带进度）
        /// </summary>
        public IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[OptimizedSceneLoader] Already loading a scene");
                yield break;
            }
            
            IsLoading = true;
            CurrentProgress = 0f;
            OnLoadingStart?.Invoke();
            
            float startTime = Time.time;
            
            // 开始异步加载
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, mode);
            
            if (loadOp == null)
            {
                Debug.LogError($"[OptimizedSceneLoader] Failed to load scene: {sceneName}");
                IsLoading = false;
                yield break;
            }
            
            // 等待加载完成
            while (!loadOp.isDone)
            {
                CurrentProgress = Mathf.Clamp01(loadOp.progress / 0.9f);
                OnLoadingProgress?.Invoke(CurrentProgress);
                yield return null;
            }
            
            // 确保最小加载时间
            float elapsed = Time.time - startTime;
            if (elapsed < minLoadingTime)
            {
                yield return new WaitForSeconds(minLoadingTime - elapsed);
            }
            
            CurrentProgress = 1f;
            OnLoadingProgress?.Invoke(1f);
            OnLoadingComplete?.Invoke();
            
            IsLoading = false;
            
            Debug.Log($"[OptimizedSceneLoader] Scene loaded: {sceneName}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 异步卸载场景
        /// </summary>
        public IEnumerator UnloadSceneAsync(string sceneName)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
            
            if (unloadOp == null)
            {
                Debug.LogWarning($"[OptimizedSceneLoader] Failed to unload scene: {sceneName}");
                yield break;
            }
            
            while (!unloadOp.isDone)
            {
                yield return null;
            }
            
            // 清理未使用的资源
            yield return Resources.UnloadUnusedAssets();
            
            Debug.Log($"[OptimizedSceneLoader] Scene unloaded: {sceneName}");
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
        
        #endregion
    }
}
