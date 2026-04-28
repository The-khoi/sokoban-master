using UnityEngine;

namespace Echoes.Puzzles
{
    /// <summary>
    /// [Echoes Mod]: 普通箱子，保持模板原有逻辑
    /// 继承 Movable 并实现 IInteractableBox 接口
    /// </summary>
    public class NormalBox : Movable, IInteractableBox
    {
        [Header("Box Settings")]
        [SerializeField] private bool canBePushed = true;

        private bool _isOnTarget = false;

        /// <summary>
        /// 箱子是否可以被推动
        /// </summary>
        public bool CanBePushed => canBePushed;

        /// <summary>
        /// 箱子是否在目标点上
        /// </summary>
        public bool IsOnTarget => _isOnTarget;

        /// <summary>
        /// 获取 Transform 组件
        /// </summary>
        public Transform Transform => transform;

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
    }
}
