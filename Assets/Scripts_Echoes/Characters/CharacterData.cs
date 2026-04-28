using UnityEngine;

namespace Echoes.Characters
{
    /// <summary>
    /// [Echoes Mod]: 角色数据 ScriptableObject
    /// 定义角色的基础属性、视觉配置和专属技能
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData_New", menuName = "Echoes/Character Data")]
    public class CharacterData : ScriptableObject
    {
        #region Identity
        
        [Header("Identity")]
        [Tooltip("角色唯一标识符")]
        [SerializeField] private int characterId;
        
        [Tooltip("角色显示名称")]
        [SerializeField] private string characterName;
        
        [Tooltip("角色描述")]
        [TextArea(3, 5)]
        [SerializeField] private string description;
        
        #endregion
        
        #region Movement
        
        [Header("Movement")]
        [Tooltip("基础移动速度乘区，1.0 = 标准速度（影响移动频率，不影响位移距离）")]
        [Range(0.5f, 2.0f)]
        [SerializeField] private float moveSpeedMultiplier = 1.0f;
        
        [Tooltip("是否可以推动箱子")]
        [SerializeField] private bool canPushBoxes = true;
        
        #endregion
        
        #region Visuals
        
        [Header("Visuals")]
        [Tooltip("角色专属视觉预制体（仅包含 Visual 层，不含逻辑组件）")]
        [SerializeField] private GameObject visualPrefab;
        
        [Tooltip("角色头像图标")]
        [SerializeField] private Sprite portraitIcon;
        
        #endregion
        
        #region Skills
        
        [Header("Skills")]
        [Tooltip("角色专属技能配置列表")]
        [SerializeField] private SkillConfig[] skills;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 角色唯一标识符
        /// </summary>
        public int CharacterId => characterId;
        
        /// <summary>
        /// 角色显示名称
        /// </summary>
        public string CharacterName => characterName;
        
        /// <summary>
        /// 角色描述
        /// </summary>
        public string Description => description;
        
        /// <summary>
        /// 移动速度乘区（影响移动频率）
        /// </summary>
        public float MoveSpeedMultiplier => moveSpeedMultiplier;
        
        /// <summary>
        /// 是否可以推动箱子
        /// </summary>
        public bool CanPushBoxes => canPushBoxes;
        
        /// <summary>
        /// 视觉预制体
        /// </summary>
        public GameObject VisualPrefab => visualPrefab;
        
        /// <summary>
        /// 头像图标
        /// </summary>
        public Sprite PortraitIcon => portraitIcon;
        
        /// <summary>
        /// 技能配置数组
        /// </summary>
        public SkillConfig[] Skills => skills;
        
        #endregion
    }
    
    /// <summary>
    /// [Echoes Mod]: 技能配置结构
    /// 将技能类型与参数配置解耦，支持运行时动态创建技能实例
    /// </summary>
    [System.Serializable]
    public struct SkillConfig
    {
        [Tooltip("技能类型（用于运行时实例化）")]
        [SerializeField] private SkillType skillType;
        
        [Tooltip("技能预制体（包含 Skill 组件的 GameObject）")]
        [SerializeField] private GameObject skillPrefab;
        
        [Tooltip("技能是否默认解锁")]
        [SerializeField] private bool unlockedByDefault;
        
        [Tooltip("自定义能量消耗（-1 表示使用技能默认值）")]
        [SerializeField] private int customEnergyCost;
        
        /// <summary>
        /// 技能类型
        /// </summary>
        public SkillType SkillType => skillType;
        
        /// <summary>
        /// 技能预制体
        /// </summary>
        public GameObject SkillPrefab => skillPrefab;
        
        /// <summary>
        /// 是否默认解锁
        /// </summary>
        public bool UnlockedByDefault => unlockedByDefault;
        
        /// <summary>
        /// 自定义能量消耗
        /// </summary>
        public int CustomEnergyCost => customEnergyCost;
    }
    
    /// <summary>
    /// [Echoes Mod]: 技能类型枚举
    /// 定义游戏中所有可用的技能类型
    /// </summary>
    public enum SkillType
    {
        None = 0,
        
        // 已实现的技能
        TimeRewind = 1,     // 时间回溯 - 撤销最近3步操作
        
        // 规划中技能
        IceFreeze = 2,      // 冰封固化 - 将动态箱子转变为静态地形
        BurstPush = 10,     // 爆裂推进 - 箱子直线滑行直至撞墙
        PhaseShift = 11,    // 相位转移
        EchoClone = 12,     // 回响分身
        GravityWell = 13,   // 重力井
        TimeFreeze = 14,    // 时间冻结
    }
}
