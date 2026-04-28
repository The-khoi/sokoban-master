using UnityEngine;

namespace Echoes.Puzzles
{
    /// <summary>
    /// [Echoes Mod]: 箱子接口，为递归传送门铺垫
    /// 定义所有可交互箱子的行为契约
    /// </summary>
    public interface IInteractableBox
    {
        /// <summary>
        /// 箱子是否可以被推动（受冰封状态影响）
        /// </summary>
        bool CanBePushed { get; }

        /// <summary>
        /// 箱子当前是否处于冰封状态
        /// </summary>
        bool IsFrozen { get; }

        /// <summary>
        /// [Echoes Mod]: 设置箱子的可移动状态（冰封固化技能调用）
        /// </summary>
        /// <param name="movable">true = 可移动；false = 冰封（静态地形）</param>
        void SetMovable(bool movable);

        /// <summary>
        /// 当箱子被推动时调用
        /// </summary>
        /// <param name="direction">推动方向</param>
        void OnPushed(Vector2 direction);

        /// <summary>
        /// 当箱子到达目标点时调用
        /// </summary>
        void OnReachedTarget();

        /// <summary>
        /// 当箱子离开目标点时调用
        /// </summary>
        void OnLeftTarget();

        /// <summary>
        /// 获取箱子的 Transform 组件
        /// </summary>
        Transform Transform { get; }
    }
}
