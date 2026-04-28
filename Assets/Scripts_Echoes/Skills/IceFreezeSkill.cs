using Echoes.Puzzles;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 冰封固化技能
    /// 消耗 2 格能量，将玩家正前方的动态箱子转变为静态地形。
    ///
    /// 实现要点：
    ///   - 通过射线检测（Raycast）精确命中正前方一格的箱子，而非范围搜索
    ///   - 调用 IInteractableBox.SetMovable(false) 冻结，不直接操作 Transform 或 Collider
    ///   - 冻结状态由 NormalBox.CanMove() 拦截，自动接入模板的碰撞检测体系
    ///   - 触发 Visual 层上的冰冻特效（若存在）
    ///   - 触发角色动画（通过基类 TriggerAnimation，不直接持有 Animator）
    ///
    /// 注意：冰封操作本身不产生 MoveCommand，因此不会被时间回溯撤销。
    /// 若需要支持撤销冰封，可在此处扩展一个 FreezeCommand。
    /// </summary>
    public class IceFreezeSkill : Skill
    {
        #region Constants

        /// <summary>射线检测距离：正好一格（与 Movable.DefaultDistance 对齐）</summary>
        private const float RAYCAST_DISTANCE = Movable.DefaultDistance;

        /// <summary>冰冻动画参数名（需在 AnimatorController 中配置同名 Bool）</summary>
        private const string ANIM_FREEZE = "UseIceFreeze";

        #endregion

        #region Fields

        [Header("Ice Freeze Settings")]
        [Tooltip("冰冻特效预制体（挂载在目标箱子位置，可为 null）")]
        [SerializeField] private GameObject freezeEffectPrefab;

        [Tooltip("射线检测层级（应包含箱子所在层）")]
        [SerializeField] private LayerMask boxLayerMask = ~0;   // 默认检测所有层

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            skillName  = "冰封固化";
            energyCost = 2;
        }

        #endregion

        #region Skill Overrides

        /// <summary>
        /// [Echoes Mod]: 前置检查：正前方一格内必须有可冰封的箱子
        /// </summary>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;

            if (FindTargetBox() == null)
            {
                Debug.LogWarning("[IceFreezeSkill] 正前方没有可冰封的箱子。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// [Echoes Mod]: 执行冰封固化
        /// </summary>
        protected override void ExecuteSkill()
        {
            IInteractableBox target = FindTargetBox();

            if (target == null)
            {
                Debug.LogWarning("[IceFreezeSkill] ExecuteSkill：未找到目标箱子，技能取消。");
                return;
            }

            // ── 1. 通过接口冻结箱子（不直接操作 Transform / Collider）──────────
            target.SetMovable(false);

            // ── 2. 播放冰冻特效（Visual 层，可选）────────────────────────────────
            SpawnFreezeEffect(target.Transform.position);

            // ── 3. 触发角色动画（通过基类，不直接持有 Animator）─────────────────
            TriggerAnimation(ANIM_FREEZE, true);

            Debug.Log($"[IceFreezeSkill] 已冰封箱子：{target.Transform.gameObject.name} @ {target.Transform.position}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 向玩家正前方发射射线，返回命中的第一个 IInteractableBox（未冻结且可推动）。
        /// 朝向由玩家 Animator 的 MoveX/MoveY 参数决定，默认向右。
        /// </summary>
        private IInteractableBox FindTargetBox()
        {
            Vector2 facing = GetFacingDirection();
            Vector2 origin = (Vector2)transform.position + facing * 0.1f;   // 略微偏移避免自身碰撞

            // 射线检测：只取第一个命中
            RaycastHit2D hit = Physics2D.Raycast(origin, facing, RAYCAST_DISTANCE, boxLayerMask);

            if (hit.collider == null) return null;

            IInteractableBox box = hit.collider.GetComponent<IInteractableBox>();

            // 已冻结的箱子不能再次冰封
            if (box == null || box.IsFrozen)
            {
                Debug.Log("[IceFreezeSkill] 射线命中目标，但已冻结或不是 IInteractableBox，跳过。");
                return null;
            }

            return box;
        }

        /// <summary>
        /// 在目标位置生成冰冻特效（若预制体已配置）
        /// </summary>
        private void SpawnFreezeEffect(Vector3 position)
        {
            if (freezeEffectPrefab == null) return;

            Instantiate(freezeEffectPrefab, position, Quaternion.identity);
            Debug.Log($"[IceFreezeSkill] 冰冻特效已生成：{position}");
        }

        /// <summary>
        /// 从玩家 Animator 读取 MoveX/MoveY 参数，推导当前朝向。
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

            // 对齐到四方向（网格游戏不需要斜向）
            return Mathf.Abs(x) >= Mathf.Abs(y)
                ? new Vector2(Mathf.Sign(x), 0f)
                : new Vector2(0f, Mathf.Sign(y));
        }

        #endregion
    }
}
