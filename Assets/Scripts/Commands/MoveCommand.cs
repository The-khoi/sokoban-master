using Audio;
using UnityEngine;

namespace Commands
{
    /// <summary>
    /// 移动命令
    /// 封装单次移动动作（含可选的箱子联动），支持精确的位置快照撤销。
    ///
    /// 设计说明：
    /// - 同时记录 Direction 枚举（兼容旧有 Clone/Redo 逻辑）和 Vector2 起止坐标（供调试与技能系统读取）。
    /// - 若本次移动推动了箱子，额外记录箱子引用及其起止坐标，Undo 时一并还原。
    /// - 坐标在 ExecuteCommand 执行前后分别采样，确保精确性。
    /// </summary>
    public class MoveCommand : Command
    {
        #region Fields

        // --- 移动实体 ---
        private readonly Movable _movable;
        private readonly Direction _direction;
        private readonly Vector3 _directionVector;
        private readonly float _distance;

        // --- 位置快照（Vector2，2D 网格精确记录）---
        /// <summary>移动前的起始位置</summary>
        private Vector2 _startPosition;
        /// <summary>移动后的目标位置</summary>
        private Vector2 _endPosition;

        // --- 箱子联动（可选）---
        /// <summary>被推动的箱子引用，未推箱子时为 null</summary>
        private Movable _pushedBox;
        /// <summary>箱子被推前的起始位置</summary>
        private Vector2 _boxStartPosition;
        /// <summary>箱子被推后的目标位置</summary>
        private Vector2 _boxEndPosition;

        #endregion

        #region Properties

        /// <summary>移动实体（玩家或箱子）</summary>
        public Movable Actor => _movable;

        /// <summary>移动方向枚举</summary>
        public Direction MoveDirection => _direction;

        /// <summary>移动前的起始位置（2D 网格坐标）</summary>
        public Vector2 StartPosition => _startPosition;

        /// <summary>移动后的目标位置（2D 网格坐标）</summary>
        public Vector2 EndPosition => _endPosition;

        /// <summary>被推动的箱子（无则为 null）</summary>
        public Movable PushedBox => _pushedBox;

        /// <summary>箱子被推前的位置（无箱子时为 Vector2.zero）</summary>
        public Vector2 BoxStartPosition => _boxStartPosition;

        /// <summary>箱子被推后的位置（无箱子时为 Vector2.zero）</summary>
        public Vector2 BoxEndPosition => _boxEndPosition;

        /// <summary>本次移动是否推动了箱子</summary>
        public bool HasPushedBox => _pushedBox != null;

        #endregion

        #region Constructor

        /// <summary>
        /// 构造移动命令
        /// </summary>
        /// <param name="movable">执行移动的实体（通常为 Player）</param>
        /// <param name="direction">移动方向</param>
        /// <param name="distance">移动距离（通常为 Movable.DefaultDistance = 1f）</param>
        public MoveCommand(Movable movable, Direction direction, float distance)
        {
            _movable = movable;
            _direction = direction;
            _directionVector = DirectionToVector(direction);
            _distance = distance;
        }

        #endregion

        #region Command Implementation

        /// <summary>
        /// 执行移动逻辑，并在执行前后采样位置快照。
        /// </summary>
        protected override void ExecuteCommand()
        {
            if (_movable == null) return;

            // 采样起始位置
            _startPosition = _movable.transform.position;

            if (_movable.CanMove(_directionVector, _distance))
            {
                // --- 玩家可直接移动 ---
                _movable.Move(_directionVector, _distance);
                _endPosition = _movable.transform.position;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayPlayerMoveSfx();
            }
            else
            {
                // --- 检查前方是否有可推动的箱子 ---
                GameObject obstacle = _movable.GetObstacle(_directionVector, _distance);

                if (obstacle != null
                    && obstacle.TryGetComponent(out Movable movableObstacle)
                    && !ReferenceEquals(_movable, movableObstacle)
                    && movableObstacle.CanMove(_directionVector, _distance))
                {
                    // 记录箱子起始位置
                    _boxStartPosition = movableObstacle.transform.position;

                    // 推动箱子（通过子命令执行，子命令会自动入栈）
                    new MoveCommand(movableObstacle, _direction, _distance).Execute();

                    // 记录箱子目标位置 & 箱子引用
                    _pushedBox = movableObstacle;
                    _boxEndPosition = movableObstacle.transform.position;

                    // 玩家跟进
                    _movable.Move(_directionVector, _distance, force: true);
                    _endPosition = _movable.transform.position;

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayCrateMoveSfx();
                }
                // 无法移动：起止位置相同，不做任何操作
                else
                {
                    _endPosition = _startPosition;
                }
            }
        }

        /// <summary>
        /// 撤销移动：将实体精确还原到起始位置。
        /// 若推动了箱子，箱子也一并还原（直接设置位置，不走物理检测）。
        /// </summary>
        public override void Undo()
        {
            if (_movable == null) return;

            // 直接还原到快照位置，避免反向射线检测的误差
            _movable.transform.position = new Vector3(_startPosition.x, _startPosition.y,
                _movable.transform.position.z);

            // 注意：箱子的撤销由其自身的 MoveCommand（子命令）负责，
            // CommandHistoryHandler 会按入栈顺序依次 Undo，无需在此重复处理。
            // _pushedBox 引用仅供外部系统（如 GameStateRecorder）读取。

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.undoSfx);
        }

        /// <summary>
        /// 重做：重新执行命令逻辑
        /// </summary>
        public override void Redo()
        {
            ExecuteCommand();
        }

        /// <summary>
        /// 克隆命令（用于长按重复执行）
        /// </summary>
        public override Command Clone()
        {
            return new MoveCommand(_movable, _direction, _distance);
        }

        #endregion

        #region Static Helpers

        private static Direction GetOppositeDirection(Direction direction)
        {
            return direction switch
            {
                Direction.Up    => Direction.Down,
                Direction.Down  => Direction.Up,
                Direction.Left  => Direction.Right,
                Direction.Right => Direction.Left,
                _               => Direction.None
            };
        }

        private static Vector3 DirectionToVector(Direction direction)
        {
            return direction switch
            {
                Direction.Up    => Vector3.up,
                Direction.Down  => Vector3.down,
                Direction.Left  => Vector3.left,
                Direction.Right => Vector3.right,
                _               => Vector3.zero
            };
        }

        #endregion
    }

    /// <summary>
    /// 移动方向枚举
    /// </summary>
    public enum Direction { Up, Down, Left, Right, None }
}
