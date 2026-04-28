using Commands;
using Echoes.Puzzles;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 爆裂推进技能
    /// 消耗 2 格能量，使玩家正前方的箱子沿直线滑行，直到撞墙或遇到障碍为止。
    ///
    /// 实现要点：
    ///   - 通过射线检测命中正前方一格的箱子
    ///   - 用 while 循环调用 Movable.CanMove() 逐格探测最大滑行距离
    ///   - 每一格位移通过 new MoveCommand(box, direction, 1f).Execute() 执行
    ///     → MoveCommand.Execute() 内部自动调用 CommandHistoryHandler.AddCommand()
    ///     → 每一格都是独立的可撤销命令，完整接入时间回溯系统
    ///   - 技能本身不直接修改任何 Transform
    ///   - 触发角色动画（通过基类 TriggerAnimation）
    /// </summary>
    public class BurstPushSkill : Skill
    {
        #region Constants

        /// <summary>单格移动距离，与 Movable.DefaultDistance 对齐</summary>
        private const float STEP = Movable.DefaultDistance;

        /// <summary>最大滑行格数上限，防止无限循环（理论上不会触发，但作为安全阀）</summary>
        private const int MAX_SLIDE_STEPS = 50;

        /// <summary>爆裂动画参数名（需在 AnimatorController 中配置同名 Bool）</summary>
        private const string ANIM_BURST = "UseBurstPush";

        #endregion

        #region Fields

        [Header("Burst Push Settings")]
        [Tooltip("爆裂特效预制体（生成在箱子初始位置，可为 null）")]
        [SerializeField] private GameObject burstEffectPrefab;

        [Tooltip("射线检测层级（应包含箱子所在层）")]
        [SerializeField] private LayerMask boxLayerMask = ~0;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            skillName  = "爆裂推进";
            energyCost = 2;
        }

        #endregion

        #region Skill Overrides

        /// <summary>
        /// [Echoes Mod]: 前置检查：正前方一格内必须有可推动的箱子
        /// </summary>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;

            if (FindTargetBox() == null)
            {
                Debug.LogWarning("[BurstPushSkill] 正前方没有可推动的箱子。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// [Echoes Mod]: 执行爆裂推进
        /// </summary>
        protected override void ExecuteSkill()
        {
            Movable targetBox = FindTargetBox();

            if (targetBox == null)
            {
                Debug.LogWarning("[BurstPushSkill] ExecuteSkill：未找到目标箱子，技能取消。");
                return;
            }

            Vector2 facing    = GetFacingDirection();
            Direction cmdDir  = Vector2ToDirection(facing);
            Vector3 dir3      = new Vector3(facing.x, facing.y, 0f);

            // ── 1. 播放爆裂特效（在箱子初始位置）────────────────────────────────
            SpawnBurstEffect(targetBox.transform.position);

            // ── 2. 触发角色动画（通过基类，不直接持有 Animator）─────────────────
            TriggerAnimation(ANIM_BURST, true);

            // ── 3. 逐格滑行，每格通过 MoveCommand 执行（自动接入撤销系统）────────
            int steps = 0;

            while (steps < MAX_SLIDE_STEPS && targetBox.CanMove(dir3, STEP))
            {
                // MoveCommand.Execute() 内部：
                //   ExecuteCommand() → box.Move(dir, distance)
                //   CommandHistoryHandler.Instance.AddCommand(this)  ← 自动入栈
                new MoveCommand(targetBox, cmdDir, STEP).Execute();
                steps++;
            }

            if (steps == 0)
            {
                Debug.LogWarning("[BurstPushSkill] 箱子无法向该方向移动（已紧贴障碍）。");
            }
            else
            {
                Debug.Log($"[BurstPushSkill] 箱子滑行了 {steps} 格，停止于 {targetBox.transform.position}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 向玩家正前方发射射线，返回命中的第一个可推动 Movable（非 Player、非冻结箱子）。
        /// </summary>
        private Movable FindTargetBox()
        {
            Vector2 facing = GetFacingDirection();
            Vector2 origin = (Vector2)transform.position + facing * 0.1f;

            RaycastHit2D hit = Physics2D.Raycast(origin, facing, STEP, boxLayerMask);

            if (hit.collider == null) return null;

            // 排除玩家自身
            if (hit.collider.GetComponent<Player>() != null) return null;

            Movable box = hit.collider.GetComponent<Movable>();
            if (box == null) return null;

            // 冻结的箱子不可推动
            IInteractableBox interactable = hit.collider.GetComponent<IInteractableBox>();
            if (interactable != null && interactable.IsFrozen)
            {
                Debug.Log("[BurstPushSkill] 目标箱子已冻结，无法推动。");
                return null;
            }

            return box;
        }

        /// <summary>
        /// 在指定位置生成爆裂特效（若预制体已配置）
        /// </summary>
        private void SpawnBurstEffect(Vector3 position)
        {
            if (burstEffectPrefab == null) return;

            Instantiate(burstEffectPrefab, position, Quaternion.identity);
            Debug.Log($"[BurstPushSkill] 爆裂特效已生成：{position}");
        }

        /// <summary>
        /// 从玩家 Animator 读取 MoveX/MoveY 参数，推导当前朝向（对齐四方向）。
        /// 若无法获取则默认向右。
        /// </summary>
        private Vector2 GetFacingDirection()
        {
            Player player = GetPlayer();
            if (player == null) return Vector2.right;

            Animator animator = player.CurrentAnimator;
            if (animator == null) return Vector2.right;

            float x = animator.GetFloat("MoveX");
            float y = animator.GetFloat("MoveY");

            if (x == 0f && y == 0f) return Vector2.right;

            return Mathf.Abs(x) >= Mathf.Abs(y)
                ? new Vector2(Mathf.Sign(x), 0f)
                : new Vector2(0f, Mathf.Sign(y));
        }

        /// <summary>
        /// 将四方向 Vector2 转换为 Direction 枚举（供 MoveCommand 构造函数使用）
        /// </summary>
        private static Direction Vector2ToDirection(Vector2 dir)
        {
            if (dir == Vector2.up)    return Direction.Up;
            if (dir == Vector2.down)  return Direction.Down;
            if (dir == Vector2.left)  return Direction.Left;
            return Direction.Right;   // 默认向右（含 Vector2.right）
        }

        #endregion
    }
}
