using Echoes.Puzzles;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 冰封固化技能
    /// 消耗2格能量，将一个动态箱子转变为静态地形
    /// 用于阻挡心魔或填坑
    /// </summary>
    public class IceFreezeSkill : Skill
    {
        [Header("Ice Freeze Settings")]
        [SerializeField] private float freezeRange = 1.5f;
        [SerializeField] private GameObject freezeEffectPrefab;
        
        protected virtual void Awake()
        {
            skillName = "Ice Freeze";
            energyCost = 2;
        }
        
        /// <summary>
        /// [Echoes Mod]: 检查是否可以执行冰封
        /// </summary>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            
            // 检查范围内是否有可冰封的箱子
            if (!HasFreezableBoxInRange())
            {
                Debug.LogWarning("[IceFreezeSkill] No freezable box in range.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 执行冰封固化
        /// </summary>
        protected override void ExecuteSkill()
        {
            // 查找范围内的可冰封箱子
            var boxes = FindObjectsOfType<Movable>();
            Movable targetBox = null;
            
            foreach (var box in boxes)
            {
                // 排除玩家自身
                if (box is Player) continue;
                
                // 检查是否是可交互箱子
                var interactable = box.GetComponent<IInteractableBox>();
                if (interactable == null) continue;
                
                // 检查距离
                float distance = Vector2.Distance(transform.position, box.transform.position);
                if (distance <= freezeRange)
                {
                    targetBox = box;
                    break; // 找到第一个就使用
                }
            }
            
            if (targetBox == null)
            {
                Debug.LogWarning("[IceFreezeSkill] No target box found.");
                return;
            }
            
            // 执行冰封
            FreezeBox(targetBox);
            
            Debug.Log($"[IceFreezeSkill] Frozen box at {targetBox.transform.position}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 冰封箱子
        /// </summary>
        private void FreezeBox(Movable box)
        {
            // 禁用箱子的移动能力（使其变成静态）
            // 方法1：禁用 BoxCollider2D 的触发器或调整碰撞层
            var collider = box.GetComponent<UnityEngine.Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // 方法2：添加 FrozenBox 标记组件
            var frozenBox = box.gameObject.AddComponent<FrozenBox>();
            frozenBox.Initialize(box);
            
            // 播放特效
            if (freezeEffectPrefab != null)
            {
                Instantiate(freezeEffectPrefab, box.transform.position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 检查范围内是否有可冰封的箱子
        /// </summary>
        private bool HasFreezableBoxInRange()
        {
            var boxes = FindObjectsOfType<Movable>();
            
            foreach (var box in boxes)
            {
                if (box is Player) continue;
                
                var interactable = box.GetComponent<IInteractableBox>();
                if (interactable == null) continue;
                
                float distance = Vector2.Distance(transform.position, box.transform.position);
                if (distance <= freezeRange)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// [Echoes Mod]: 冰封箱子标记组件
    /// 标记箱子已被冰封，可用于后续解除冰封
    /// </summary>
    public class FrozenBox : MonoBehaviour
    {
        private Movable _originalBox;
        private Vector2 _frozenPosition;
        
        public void Initialize(Movable box)
        {
            _originalBox = box;
            _frozenPosition = box.transform.position;
            
            // 改变颜色表示冰封状态
            var spriteRenderer = box.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.7f, 0.9f, 1f, 0.8f); // 冰蓝色
            }
            
            Debug.Log($"[FrozenBox] Box frozen at {_frozenPosition}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 解除冰封
        /// </summary>
        public void Unfreeze()
        {
            if (_originalBox != null)
            {
                var collider = _originalBox.GetComponent<UnityEngine.Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true;
                }
                
                // 恢复颜色
                var spriteRenderer = _originalBox.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
                
                Destroy(this);
                Debug.Log("[FrozenBox] Box unfrozen.");
            }
        }
    }
}