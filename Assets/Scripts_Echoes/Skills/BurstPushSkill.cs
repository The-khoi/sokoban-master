using Commands;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 爆裂推进技能
    /// 消耗2格能量，使箱子直线滑行直至撞墙
    /// 用于远距离位移
    /// </summary>
    public class BurstPushSkill : Skill
    {
        [Header("Burst Push Settings")]
        [SerializeField] private float pushDistance = 3f;
        [SerializeField] private GameObject burstEffectPrefab;
        
        protected virtual void Awake()
        {
            skillName = "Burst Push";
            energyCost = 2;
        }
        
        /// <summary>
        /// [Echoes Mod]: 检查是否可以执行爆裂推进
        /// </summary>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            
            // 检查前方是否有可推动的箱子
            if (!HasPushableBoxInFront())
            {
                Debug.LogWarning("[BurstPushSkill] No pushable box in front.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 执行爆裂推进
        /// </summary>
        protected override void ExecuteSkill()
        {
            // 获取玩家朝向（基于最近的移动方向）
            Vector2 pushDirection = GetPlayerFacingDirection();
            
            if (pushDirection == Vector2.zero)
            {
                Debug.LogWarning("[BurstPushSkill] Cannot determine push direction.");
                return;
            }
            
            // 查找前方的箱子
            var boxes = FindObjectsOfType<Movable>();
            Movable targetBox = null;
            
            foreach (var box in boxes)
            {
                if (box is Player) continue;
                
                // 检查箱子是否在玩家前方
                Vector2 toBox = (Vector2)box.transform.position - (Vector2)transform.position;
                float dotProduct = Vector2.Dot(toBox.normalized, pushDirection);
                
                // 在前方且距离较近
                if (dotProduct > 0.5f && toBox.magnitude <= 1.5f)
                {
                    targetBox = box;
                    break;
                }
            }
            
            if (targetBox == null)
            {
                Debug.LogWarning("[BurstPushSkill] No target box found.");
                return;
            }
            
            // 执行爆裂推进
            BurstPushBox(targetBox, pushDirection);
            
            Debug.Log($"[BurstPushSkill] Burst pushed box in direction {pushDirection}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 爆裂推进箱子
        /// </summary>
        private void BurstPushBox(Movable box, Vector2 direction)
        {
            // 计算最大推动距离（直到撞墙）
            float actualDistance = CalculateBurstDistance(box, direction);
            
            if (actualDistance <= 0)
            {
                Debug.LogWarning("[BurstPushSkill] Box cannot move in this direction.");
                return;
            }
            
            // 强制移动箱子
            box.Move(direction, actualDistance, force: true);
            
            // 播放特效
            if (burstEffectPrefab != null)
            {
                Vector3 effectPos = box.transform.position - (Vector3)direction * 0.5f;
                Instantiate(burstEffectPrefab, effectPos, Quaternion.identity);
            }
            
            // 记录移动命令（用于撤销）
            var moveCommand = new MoveCommand(box, DirectionToEnum(direction), actualDistance);
            CommandHistoryHandler.Instance.AddCommand(moveCommand);
        }
        
        /// <summary>
        /// [Echoes Mod]: 计算箱子可以滑行的距离
        /// </summary>
        private float CalculateBurstDistance(Movable box, Vector2 direction)
        {
            float maxDistance = pushDistance;
            float step = 0.5f;
            float actualDistance = 0;
            
            for (float d = step; d <= maxDistance; d += step)
            {
                Vector2 testPos = (Vector2)box.transform.position + direction * d;
                
                // 检查该位置是否有障碍
                if (IsPositionBlocked(testPos))
                {
                    break;
                }
                
                actualDistance = d;
            }
            
            return actualDistance;
        }
        
        /// <summary>
        /// [Echoes Mod]: 检查位置是否有障碍
        /// </summary>
        private bool IsPositionBlocked(Vector2 position)
        {
            // 使用物理检测
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.3f);
            
            foreach (var collider in colliders)
            {
                // 忽略箱子自身和玩家
                if (collider.GetComponent<Movable>() != null)
                    continue;
                    
                // 检查是否是障碍层
                if (collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// [Echoes Mod]: 获取玩家朝向
        /// </summary>
        private Vector2 GetPlayerFacingDirection()
        {
            // 简单实现：基于玩家当前移动状态
            // 实际可以通过 Animator 参数或保存的移动方向来确定
            var player = GetComponent<Player>();
            if (player != null)
            {
                // 尝试从 Animator 获取方向
                var animator = player.GetComponent<UnityEngine.Animator>();
                if (animator != null)
                {
                    float moveX = animator.GetFloat("MoveX");
                    float moveY = animator.GetFloat("MoveY");
                    
                    if (moveX != 0 || moveY != 0)
                    {
                        return new Vector2(moveX, moveY).normalized;
                    }
                }
            }
            
            // 默认向右
            return Vector2.right;
        }
        
        /// <summary>
        /// [Echoes Mod]: Vector2 转换为 Direction 枚举
        /// </summary>
        private Direction DirectionToEnum(Vector2 direction)
        {
            if (direction == Vector2.up) return Direction.Up;
            if (direction == Vector2.down) return Direction.Down;
            if (direction == Vector2.left) return Direction.Left;
            if (direction == Vector2.right) return Direction.Right;
            return Direction.Right;
        }
        
        /// <summary>
        /// [Echoes Mod]: 检查前方是否有可推动的箱子
        /// </summary>
        private bool HasPushableBoxInFront()
        {
            Vector2 direction = GetPlayerFacingDirection();
            var boxes = FindObjectsOfType<Movable>();
            
            foreach (var box in boxes)
            {
                if (box is Player) continue;
                
                Vector2 toBox = (Vector2)box.transform.position - (Vector2)transform.position;
                float dotProduct = Vector2.Dot(toBox.normalized, direction);
                
                if (dotProduct > 0.5f && toBox.magnitude <= 1.5f)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}