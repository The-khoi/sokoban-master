using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echoes.Puzzles
{
    /// <summary>
    /// [Echoes Mod]: 传送门箱子，为递归嵌套铺垫
    /// 支持内外联动：当内层场景完成时，通知父层改变传送门状态
    /// </summary>
    public class PortalBox : Movable, IInteractableBox
    {
        #region Events
        
        /// <summary>
        /// [Echoes Mod]: 当传送门完成状态改变时触发
        /// 参数：(portalBoxId, isCompleted)
        /// </summary>
        public static event Action<int, bool> OnPortalCompletedChanged;
        
        #endregion
        
        #region Fields
        
        [Header("Portal Settings")]
        [SerializeField] private bool canBePushed = true;
        [SerializeField] private string targetSceneName;
        [SerializeField] private bool isActive = true;
        
        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 1.5f;
        [SerializeField] private bool showInteractionPrompt = true;
        
        [Header("Completion Settings")]
        [SerializeField] private bool isCompleted = false;
        [SerializeField] private GameObject completedVisual;  // 完成后的视觉效果
        [SerializeField] private GameObject normalVisual;      // 正常状态的视觉效果
        
        private bool _isOnTarget = false;
        private bool _playerInRange = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 箱子是否可以被推动
        /// </summary>
        public bool CanBePushed => canBePushed && isActive;
        
        /// <summary>
        /// [Echoes Mod]: 传送门箱子不支持冰封，始终返回 false
        /// </summary>
        public bool IsFrozen => false;
        
        /// <summary>
        /// [Echoes Mod]: 传送门箱子不支持冰封固化，调用此方法无效
        /// </summary>
        public void SetMovable(bool movable)
        {
            // 传送门箱子不参与冰封逻辑，忽略此调用
            Debug.LogWarning($"[PortalBox] SetMovable 在传送门箱子上无效，已忽略。");
        }
        
        /// <summary>
        /// 箱子是否在目标点上
        /// </summary>
        public bool IsOnTarget => _isOnTarget;
        
        /// <summary>
        /// 传送门是否激活
        /// </summary>
        public bool IsActive => isActive;
        
        /// <summary>
        /// 获取 Transform 组件
        /// </summary>
        public Transform Transform => transform;
        
        /// <summary>
        /// 目标场景名称
        /// </summary>
        public string TargetSceneName => targetSceneName;
        
        /// <summary>
        /// 玩家是否在交互范围内
        /// </summary>
        public bool PlayerInRange => _playerInRange;
        
        /// <summary>
        /// [Echoes Mod]: 内层场景是否已完成
        /// </summary>
        public bool IsCompleted => isCompleted;
        
        /// <summary>
        /// [Echoes Mod]: 传送门唯一标识（用于跨场景匹配）
        /// </summary>
        public int PortalId => GetInstanceID();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            // 监听场景栈事件
            Echoes.Core.SceneStackManager.OnInnerSceneCompleted += OnInnerSceneCompleted;
        }
        
        private void OnDisable()
        {
            Echoes.Core.SceneStackManager.OnInnerSceneCompleted -= OnInnerSceneCompleted;
        }
        
        private void Start()
        {
            UpdateVisuals();
        }
        
        private void OnGUI()
        {
            if (showInteractionPrompt && _playerInRange && isActive && !string.IsNullOrEmpty(targetSceneName) && !isCompleted)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                
                Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.5f);
                Rect rect = new Rect(screenPos.x - 60, Screen.height - screenPos.y - 30, 120, 25);
                GUI.Label(rect, "Press E to Enter", style);
            }
        }
        
        #endregion
        
        #region Trigger Detection
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInRange = true;
                Debug.Log($"[PortalBox] Player entered range: {gameObject.name}");
            }
        }
        
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInRange = false;
                Debug.Log($"[PortalBox] Player exited range: {gameObject.name}");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// [Echoes Mod]: Unity Input System 回调 - 进入传送门
        /// </summary>
        public void OnInputEnterPortal(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (!_playerInRange) return;
            if (isCompleted)
            {
                Debug.Log($"[PortalBox] Portal already completed: {gameObject.name}");
                return;
            }
            
            EnterPortal();
        }
        
        /// <summary>
        /// [Echoes Mod]: 当箱子被推动时调用
        /// </summary>
        public virtual void OnPushed(Vector2 direction)
        {
            // 可添加推动特效
        }
        
        /// <summary>
        /// [Echoes Mod]: 当箱子到达目标点时调用
        /// </summary>
        public virtual void OnReachedTarget()
        {
            _isOnTarget = true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 当箱子离开目标点时调用
        /// </summary>
        public virtual void OnLeftTarget()
        {
            _isOnTarget = false;
        }
        
        /// <summary>
        /// [Echoes Mod]: 进入传送门，触发场景切换
        /// </summary>
        public virtual void EnterPortal()
        {
            if (!isActive || string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning("[PortalBox] Portal is not active or target scene not set.");
                return;
            }
            
            if (Echoes.Core.SceneStackManager.Instance != null)
            {
                Debug.Log($"[PortalBox] Entering portal to scene: {targetSceneName}");
                Echoes.Core.SceneStackManager.Instance.EnterInnerScene(targetSceneName, this);
            }
            else
            {
                Debug.LogWarning("[PortalBox] SceneStackManager not found, loading scene directly.");
                SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 从内层场景返回时调用
        /// </summary>
        public virtual void OnReturnFromInnerScene()
        {
            Debug.Log($"[PortalBox] Player returned from inner scene.");
        }
        
        /// <summary>
        /// [Echoes Mod]: 设置完成状态
        /// </summary>
        public void SetCompleted(bool completed)
        {
            if (isCompleted == completed) return;
            
            isCompleted = completed;
            UpdateVisuals();
            
            // 触发事件通知其他系统
            OnPortalCompletedChanged?.Invoke(PortalId, isCompleted);
            
            Debug.Log($"[PortalBox] Portal {gameObject.name} completed state: {isCompleted}");
        }
        
        /// <summary>
        /// 激活传送门
        /// </summary>
        public void Activate()
        {
            isActive = true;
        }
        
        /// <summary>
        /// 停用传送门
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
        }
        
        /// <summary>
        /// 设置目标场景
        /// </summary>
        public void SetTargetScene(string sceneName)
        {
            targetSceneName = sceneName;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// [Echoes Mod]: 内层场景完成事件处理
        /// </summary>
        private void OnInnerSceneCompleted(int portalBoxId, bool completed)
        {
            // 检查是否是自己的内层场景
            if (portalBoxId == PortalId)
            {
                SetCompleted(completed);
                Debug.Log($"[PortalBox] Received completion notification for portal {gameObject.name}");
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 更新视觉效果
        /// </summary>
        private void UpdateVisuals()
        {
            if (completedVisual != null)
                completedVisual.SetActive(isCompleted);
            
            if (normalVisual != null)
                normalVisual.SetActive(!isCompleted);
        }
        
        #endregion
        
        #region Gizmos
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isCompleted ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
        
        #endregion
    }
}
