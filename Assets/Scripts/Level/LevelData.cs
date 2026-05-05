using UnityEngine;

namespace Level
{
    /// <summary>
    /// [Echoes Mod]: 关卡元数据 ScriptableObject
    /// 包装原有 TextAsset，附加名称、难度、步数等信息
    /// 与 LevelLoader 的原有 TextAsset[] 并存，不破坏原有流程
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData_New", menuName = "Level/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("关卡内部编号（从0开始，与 levels[] 数组下标对应）")]
        [SerializeField] private int levelIndex;

        [Tooltip("关卡显示名称")]
        [SerializeField] private string levelName;

        [Tooltip("关卡简短描述")]
        [TextArea(2, 3)]
        [SerializeField] private string description;

        [Tooltip("关卡文本文件（.txt），与原 TextAsset[] 中的文件相同）")]
        [SerializeField] private TextAsset levelText;

        [Header("Difficulty & Score")]
        [Range(1, 5)]
        [SerializeField] private int difficulty = 1;

        [Tooltip("标准步数（用于三星评分）")]
        [SerializeField] private int parMoves = 20;

        [Header("Unlock")]
        [SerializeField] private bool unlockedByDefault = true;

        [Tooltip("解锁此关需要先完成的关卡索引，-1 表示无前置")]
        [SerializeField] private int requiredLevelIndex = -1;

        // ── Properties ────────────────────────────────────────
        public int LevelIndex   => levelIndex;
        public string LevelName => string.IsNullOrEmpty(levelName) ? $"Level {levelIndex}" : levelName;
        public string Description => description;
        public TextAsset LevelText => levelText;
        public int Difficulty   => difficulty;
        public int ParMoves     => parMoves;
        public bool UnlockedByDefault => unlockedByDefault;
        public int RequiredLevelIndex => requiredLevelIndex;
    }
}
