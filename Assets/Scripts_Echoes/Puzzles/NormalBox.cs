using UnityEngine;

namespace Echoes.Puzzles
{
    /// <summary>
    /// [Echoes Mod]: 普通箱子，保持模板原有逻辑
    /// 继承 Movable 并实现 IInteractableBox 接口
    /// </summary>
    public class NormalBox : Movable, IInteractableBox
    {
        #region Fields

        [Header("Box Settings")]
        [SerializeField] private bool canBePushed = true;

        /// <summary>冰封状态下的颜色</summary>
        private static readonly Color FROZEN_COLOR   = new Color(0.7f, 0.9f, 1f, 0.85f);
        /// <summary>正常状态下的颜色（恢复用）</summary>
        private static readonly Color DEFAULT_COLOR  = Color.white;

        private bool _isOnTarget = false;

        // [Echoes Mod]: 冰封固化技能状态
        private bool _isFrozen = false;
        private SpriteRenderer _spriteRenderer;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        #endregion

        #region IInteractableBox

        /// <summary>
        /// 箱子是否可以被推动（冰封时返回 false）
        /// </summary>
        public bool CanBePushed => canBePushed && !_isFrozen;

        /// <summary>
        /// 箱子当前是否处于冰封状态
        /// </summary>
        public bool IsFrozen => _isFrozen;

        /// <summary>
        /// 获取 Transform 组件
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// [Echoes Mod]: 设置箱子的可移动状态（冰封固化技能调用）
        /// 冻结时：禁用 Movable 的碰撞检测响应，并更新视觉颜色
        /// 解冻时：恢复原始状态
        /// </summary>
        public virtual void SetMovable(bool movable)
        {
            _isFrozen = !movable;

            // 更新视觉颜色反馈
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _isFrozen ? FROZEN_COLOR : DEFAULT_COLOR;
            }

            Debug.Log($"[NormalBox] {gameObject.name} 冰封状态 → {_isFrozen}");
        }

        /// <summary>
        /// [Echoes Mod]: 当箱子被推动时调用
        /// </summary>
        public virtual void OnPushed(Vector2 direction)
        {
            // 普通箱子被推动时无特殊行为
            // 可由子类重写以添加音效、特效等
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

        #endregion

        #region Properties

        /// <summary>
        /// 箱子是否在目标点上
        /// </summary>
        public bool IsOnTarget => _isOnTarget;

        #endregion

        #region Movable Override

        /// <summary>
        /// [Echoes Mod]: 冰封状态下阻止任何移动
        /// </summary>
        public override bool CanMove(Vector3 direction, float distance, bool withMovable = false)
        {
            if (_isFrozen) return false;
            return base.CanMove(direction, distance, withMovable);
        }

        #endregion
    }
}
