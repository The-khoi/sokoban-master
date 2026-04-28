using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Characters
{
    /// <summary>
    /// [Echoes Mod]: 角色管理器
    /// 负责角色加载、切换、视觉层管理和技能实例化
    /// 单例模式，跨场景存活
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        #region Singleton
        
        private static CharacterManager _instance;
        public static CharacterManager Instance => _instance;
        
        #endregion
        
        #region Fields
        
        [Header("Character Database")]
        [Tooltip("所有可用角色数据")]
        [SerializeField] private CharacterData[] characterDatabase;
        
        [Header("Current Character")]
        [SerializeField] private CharacterData currentCharacter;
        
        /// <summary>
        /// 当前角色数据
        /// </summary>
        private Dictionary<int, CharacterData> _characterDict;
        
        /// <summary>
        /// 当前视觉层实例
        /// </summary>
        private GameObject _currentVisualInstance;
        
        /// <summary>
        /// 当前技能实例列表
        /// </summary>
        private List<GameObject> _skillInstances;
        
        /// <summary>
        /// 玩家引用
        /// </summary>
        private Player _player;
        
        /// <summary>
        /// 视觉挂载点
        /// </summary>
        private Transform _visualAnchor;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// [Echoes Mod]: 当角色切换时触发
        /// 参数: (新角色ID, 旧角色ID)
        /// </summary>
        public event Action<int, int> OnCharacterChanged;
        
        /// <summary>
        /// [Echoes Mod]: 当视觉层更新时触发
        /// </summary>
        public event Action<GameObject> OnVisualUpdated;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前角色数据
        /// </summary>
        public CharacterData CurrentCharacter => currentCharacter;
        
        /// <summary>
        /// 当前角色ID
        /// </summary>
        public int CurrentCharacterId => currentCharacter?.CharacterId ?? 0;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureSingleton();
            InitializeDatabase();
            _skillInstances = new List<GameObject>();
        }
        
        #endregion
        
        #region Public Methods - Initialization
        
        /// <summary>
        /// [Echoes Mod]: 设置玩家引用
        /// 由 GameManager 或 LevelLoader 调用
        /// </summary>
        public void SetPlayer(Player player)
        {
            _player = player;
            if (_player != null)
            {
                _visualAnchor = _player.VisualAnchor;
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 初始化角色（关卡开始时调用）
        /// </summary>
        public void InitializeCharacter(int characterId)
        {
            if (_characterDict == null || !_characterDict.ContainsKey(characterId))
            {
                Debug.LogError($"[CharacterManager] Character ID {characterId} not found in database.");
                return;
            }
            
            CharacterData newCharacter = _characterDict[characterId];
            SwitchCharacter(newCharacter);
        }
        
        #endregion
        
        #region Public Methods - Character Switching
        
        /// <summary>
        /// [Echoes Mod]: 切换角色
        /// </summary>
        public void SwitchCharacter(int characterId)
        {
            if (_characterDict == null || !_characterDict.ContainsKey(characterId))
            {
                Debug.LogError($"[CharacterManager] Character ID {characterId} not found.");
                return;
            }
            
            SwitchCharacter(_characterDict[characterId]);
        }
        
        /// <summary>
        /// [Echoes Mod]: 切换角色（通过 CharacterData）
        /// </summary>
        public void SwitchCharacter(CharacterData newCharacter)
        {
            if (newCharacter == null)
            {
                Debug.LogError("[CharacterManager] Cannot switch to null character.");
                return;
            }
            
            int oldCharacterId = currentCharacter?.CharacterId ?? 0;
            
            // 检查是否是同一个角色
            if (currentCharacter != null && currentCharacter.CharacterId == newCharacter.CharacterId)
            {
                Debug.Log($"[CharacterManager] Already using character: {newCharacter.CharacterName}");
                return;
            }
            
            // 清理旧视觉和技能
            CleanupCurrentVisual();
            CleanupCurrentSkills();
            
            // 更新当前角色
            currentCharacter = newCharacter;
            
            // 应用新视觉
            ApplyVisual(newCharacter);
            
            // 实例化新技能
            InstantiateSkills(newCharacter);
            
            // 通知玩家更新
            if (_player != null)
            {
                _player.SetCharacterData(newCharacter);
            }
            
            // 触发事件
            OnCharacterChanged?.Invoke(newCharacter.CharacterId, oldCharacterId);
            
            Debug.Log($"[CharacterManager] Switched to character: {newCharacter.CharacterName} (ID: {newCharacter.CharacterId})");
        }
        
        #endregion
        
        #region Public Methods - Query
        
        /// <summary>
        /// [Echoes Mod]: 获取角色数据
        /// </summary>
        public CharacterData GetCharacterData(int characterId)
        {
            if (_characterDict == null || !_characterDict.ContainsKey(characterId))
                return null;
            
            return _characterDict[characterId];
        }
        
        /// <summary>
        /// [Echoes Mod]: 获取所有可用角色
        /// </summary>
        public CharacterData[] GetAllCharacters()
        {
            return characterDatabase;
        }
        
        #endregion
        
        #region Private Methods
        
        private void EnsureSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeDatabase()
        {
            _characterDict = new Dictionary<int, CharacterData>();
            
            if (characterDatabase == null) return;
            
            foreach (var character in characterDatabase)
            {
                if (character == null) continue;
                
                if (_characterDict.ContainsKey(character.CharacterId))
                {
                    Debug.LogWarning($"[CharacterManager] Duplicate character ID: {character.CharacterId}");
                    continue;
                }
                
                _characterDict.Add(character.CharacterId, character);
            }
            
            Debug.Log($"[CharacterManager] Database initialized with {_characterDict.Count} characters.");
        }
        
        /// <summary>
        /// [Echoes Mod]: 应用视觉层
        /// 严格遵守 Visual Decoupling 原则：
        /// - 不销毁 Player Root 对象
        /// - 仅销毁/替换 Visual 子物体
        /// </summary>
        private void ApplyVisual(CharacterData character)
        {
            if (_visualAnchor == null)
            {
                Debug.LogWarning("[CharacterManager] Visual anchor is null, cannot apply visual.");
                return;
            }
            
            if (character.VisualPrefab == null)
            {
                Debug.LogWarning($"[CharacterManager] Character {character.CharacterName} has no visual prefab.");
                return;
            }
            
            // [Echoes Mod]: 销毁旧的 Visual 子物体（不销毁 Player Root）
            if (_currentVisualInstance != null)
            {
                Destroy(_currentVisualInstance);
                _currentVisualInstance = null;
                Debug.Log("[CharacterManager] Destroyed old visual child.");
            }
            
            // 实例化新视觉作为 VisualAnchor 的子物体
            _currentVisualInstance = Instantiate(character.VisualPrefab, _visualAnchor);
            _currentVisualInstance.transform.localPosition = Vector3.zero;
            _currentVisualInstance.transform.localRotation = Quaternion.identity;
            _currentVisualInstance.name = "Visual";
            
            // [Echoes Mod]: 从 Visual 实例获取 Animator 并设置给 Player
            if (_player != null)
            {
                var animator = _currentVisualInstance.GetComponent<Animator>();
                _player.SetAnimator(animator);
                Debug.Log($"[CharacterManager] Set animator from visual: {(animator != null ? "found" : "not found")}");
            }
            
            OnVisualUpdated?.Invoke(_currentVisualInstance);
            
            Debug.Log($"[CharacterManager] Applied visual for {character.CharacterName}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 实例化技能组件
        /// 严格遵守 Visual Decoupling 原则：
        /// - 移除旧的 Skill 组件（不销毁 Player Root）
        /// - 挂载新的 Skill 组件
        /// </summary>
        private void InstantiateSkills(CharacterData character)
        {
            // [Echoes Mod]: 移除旧的 Skill 组件（不销毁 Player Root）
            if (_player != null)
            {
                var oldSkills = _player.GetComponents<Core.Skill>();
                foreach (var oldSkill in oldSkills)
                {
                    Destroy(oldSkill);
                }
                Debug.Log($"[CharacterManager] Removed {oldSkills.Length} old skill components.");
            }
            
            // 清理技能实例列表
            _skillInstances.Clear();
            
            if (character.Skills == null || character.Skills.Length == 0)
            {
                Debug.Log($"[CharacterManager] Character {character.CharacterName} has no skills.");
                return;
            }
            
            foreach (var skillConfig in character.Skills)
            {
                if (skillConfig.SkillPrefab == null)
                {
                    Debug.LogWarning($"[CharacterManager] Skill prefab is null for type: {skillConfig.SkillType}");
                    continue;
                }
                
                // [Echoes Mod]: 实例化技能预制体并获取 Skill 组件
                GameObject skillInstance = Instantiate(skillConfig.SkillPrefab, _player.transform);
                skillInstance.name = $"Skill_{skillConfig.SkillType}";
                
                // 应用自定义配置
                ApplySkillConfig(skillInstance, skillConfig);
                
                _skillInstances.Add(skillInstance);
                
                Debug.Log($"[CharacterManager] Instantiated skill: {skillConfig.SkillType}");
            }
        }
        
        private void ApplySkillConfig(GameObject skillInstance, SkillConfig config)
        {
            var skill = skillInstance.GetComponent<Core.Skill>();
            if (skill == null) return;
            
            // 应用自定义能量消耗
            if (config.CustomEnergyCost >= 0)
            {
                // 通过反射或公共方法设置（需要在 Skill 基类中添加）
                // skill.SetEnergyCost(config.CustomEnergyCost);
            }
            
            // 应用解锁状态
            if (!config.UnlockedByDefault)
            {
                skill.Lock();
            }
        }
        
        private void CleanupCurrentVisual()
        {
            // [Echoes Mod]: 仅销毁 Visual 子物体，不销毁 Player Root
            if (_currentVisualInstance != null)
            {
                Destroy(_currentVisualInstance);
                _currentVisualInstance = null;
                Debug.Log("[CharacterManager] Cleaned up visual child (Player Root preserved).");
            }
        }
        
        private void CleanupCurrentSkills()
        {
            // [Echoes Mod]: 移除 Skill 组件，不销毁 Player Root
            if (_player != null)
            {
                var oldSkills = _player.GetComponents<Core.Skill>();
                foreach (var oldSkill in oldSkills)
                {
                    if (oldSkill != null)
                    {
                        Destroy(oldSkill);
                    }
                }
            }
            
            // 清理技能实例列表
            _skillInstances.Clear();
        }
        
        #endregion
    }
}
