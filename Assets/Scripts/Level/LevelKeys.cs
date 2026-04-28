using UnityEngine;

namespace Level
{
    [CreateAssetMenu(fileName = "LevelKeys", menuName = "Level/LevelKeys")]
    public class LevelKeys : ScriptableObject
    {
        [Header("Basic Keys")]
        [SerializeField] private char wallKey = '#';
        [SerializeField] private char emptySpaceKey = '.';
        [SerializeField] private char playerKey = '@';
        [SerializeField] private char crateKey = 'C';
        [SerializeField] private char targetKey = 'T';
        
        // [Echoes Mod]: 添加 PortalBox 映射键
        [Header("Echoes Mod - Portal Box")]
        [SerializeField] private char portalBoxKey = 'P';
        
        public char WallKey => wallKey;
        public char EmptySpaceKey => emptySpaceKey;
        public char PlayerKey => playerKey;
        public char CrateKey => crateKey;
        public char TargetKey => targetKey;
        
        // [Echoes Mod]: PortalBox 键访问器
        public char PortalBoxKey => portalBoxKey;
    }
}