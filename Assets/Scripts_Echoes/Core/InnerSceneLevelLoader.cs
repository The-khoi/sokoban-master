using System;
using System.Collections.Generic;
using System.Linq;
using Echoes.Puzzles;
using Level;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 内层场景关卡加载器
    /// 跳过 Player 实例化，复用外层场景的 Player 对象。
    /// 相机控制由 SceneStackManager 统一管理。
    /// </summary>
    public class InnerSceneLevelLoader : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private TextAsset innerLevelText;
        [SerializeField] private LevelKeys levelKeys;
        
        [Header("Prefabs (不含 Player)")]
        [SerializeField] private GameObject cratePrefab;
        [SerializeField] private GameObject portalBoxPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private GameObject emptySpacePrefab;
        
        [Header("Grid")]
        [SerializeField] private Grid grid;
        
        public event Action OnLevelLoaded;
        
        // [Echoes Mod]: 记录 Player 在关卡文本中的出生点
        private Vector3 _playerSpawnPosition;
        public Vector3 PlayerSpawnPosition => _playerSpawnPosition;
        
        // [Echoes Mod]: 暴露关卡边界供 SceneStackManager 使用
        private Bounds _levelBounds;
        public Bounds LevelBounds => _levelBounds;
        
        private Dictionary<char, GameObject> _tileMap;
        private Dictionary<char, GameObject> _objectMap;
        private List<GameObject> _levelObjects;
        
        private void Start()
        {
            if (grid == null)
            {
                Debug.LogError("[InnerSceneLevelLoader] Grid is not assigned.");
                return;
            }
            
            _levelObjects = new List<GameObject>();
            
            InitializeMaps();
            LoadLevel();
        }
        
        private void InitializeMaps()
        {
            if (levelKeys == null)
            {
                Debug.LogError("[InnerSceneLevelLoader] LevelKeys is not assigned.");
                return;
            }
            
            _tileMap = new Dictionary<char, GameObject>
            {
                { levelKeys.WallKey, wallPrefab },
                { levelKeys.EmptySpaceKey, emptySpacePrefab },
            };
            
            // [Echoes Mod]: _objectMap 不包含 Player，Player 由外层场景复用
            _objectMap = new Dictionary<char, GameObject>
            {
                { levelKeys.CrateKey, cratePrefab },
                { levelKeys.TargetKey, targetPrefab },
                { levelKeys.PortalBoxKey, portalBoxPrefab }
            };
        }
        
        public void LoadLevel()
        {
            if (innerLevelText == null)
            {
                Debug.LogError("[InnerSceneLevelLoader] Inner level text is not assigned.");
                return;
            }
            
            ClearLevel();
            ParseAndInstantiateLevel(innerLevelText.text);
            OnLevelLoaded?.Invoke();
        }
        
        private void ParseAndInstantiateLevel(string levelText)
        {
            string[] lines = levelText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _levelBounds = new Bounds();
            bool spawnFound = false;
            
            for (int y = 0; y < lines.Length; y++)
            {
                for (int x = 0; x < lines[y].Length; x++)
                {
                    char key = lines[y][x];
                    Vector3Int cellPos = new Vector3Int(x, -y, 0);
                    Vector3 worldPos = grid.CellToWorld(cellPos);
                    
                    // [Echoes Mod]: 记录 Player 出生点，但不实例化 Player
                    if (levelKeys != null && key == levelKeys.PlayerKey)
                    {
                        _playerSpawnPosition = worldPos;
                        spawnFound = true;
                        
                        // 在出生点放一块地板
                        if (emptySpacePrefab != null)
                        {
                            GameObject ground = Instantiate(emptySpacePrefab, worldPos, Quaternion.identity, transform);
                            _levelObjects.Add(ground);
                        }
                        
                        _levelBounds.Encapsulate(worldPos);
                        continue;
                    }
                    
                    // 生成地形
                    if (_tileMap.TryGetValue(key, out GameObject tilePrefab) && tilePrefab != null)
                    {
                        GameObject tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                        _levelObjects.Add(tile);
                        
                        if (tilePrefab == wallPrefab)
                            tile.layer = LayerMask.NameToLayer("Obstacle");
                    }
                    
                    // 生成对象
                    if (_objectMap.TryGetValue(key, out GameObject objPrefab))
                    {
                        if (emptySpacePrefab != null)
                        {
                            GameObject ground = Instantiate(emptySpacePrefab, worldPos, Quaternion.identity, transform);
                            _levelObjects.Add(ground);
                        }
                        
                        if (objPrefab != null)
                        {
                            GameObject obj = Instantiate(objPrefab, worldPos, Quaternion.identity);
                            _levelObjects.Add(obj);
                        }
                    }
                    
                    _levelBounds.Encapsulate(worldPos);
                }
            }
            
            if (!spawnFound)
                Debug.LogWarning("[InnerSceneLevelLoader] No player spawn point (@) found in level text.");
        }
        
        public void ClearLevel()
        {
            if (_levelObjects == null) return;
            
            foreach (var obj in _levelObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _levelObjects.Clear();
        }
        
        public T[] GetObjectsOfType<T>() where T : MonoBehaviour
        {
            return _levelObjects
                .Select(obj => obj.GetComponent<T>())
                .Where(comp => comp != null)
                .ToArray();
        }
    }
}