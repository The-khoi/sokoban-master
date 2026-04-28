using System.Linq;
using Audio;
using Commands;
using Echoes.Core;
using Level;
using UI;
using UnityEngine;

public class GameManager: MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private PlayerMovementController playerMovementController;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private EndMenuController endMenuController;
    
    // [Echoes Mod]: GameStateRecorder 引用
    [Header("Echoes Mod - State Recorder")]
    [SerializeField] private GameStateRecorder gameStateRecorder;
    
    public bool IsGamePaused => PauseMenuController.IsPaused;
    public bool HasNextLevel => levelLoader.HasNextLevel;
    
    private Target[] _targets;
    
    private PauseMenuController _pauseMenuController;
    
    private void Awake()
    {
        EnsureSingleton();
    }

    private void OnEnable()
    {
        levelLoader.OnLevelLoaded += OnLevelLoaded;
        _pauseMenuController = FindObjectOfType<PauseMenuController>();
    }
    
    private void OnDisable()
    {
        if (levelLoader != null)
        {
            levelLoader.OnLevelLoaded -= OnLevelLoaded;
        }
        
        if (_targets != null)
        {
            foreach (var target in _targets)
            {
                if (target != null)
                    target.OnOccupied -= OnTargetOccupied;
            }
        }
    }

    private void Start()
    {
        _pauseMenuController = FindObjectOfType<PauseMenuController>();
    }

    private bool AreAllTargetsOccupied()
    {
        return _targets.Length == 0 || _targets.All(target => target.IsOccupied);
    }
    
    private void OnTargetOccupied()
    {
        if (AreAllTargetsOccupied())
        {
            if (endMenuController != null && _pauseMenuController)
            {
                _pauseMenuController.Pause(false);
                endMenuController.DisplayEndMenu();
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySfx(AudioManager.Instance.levelCompleteSfx);
            }
            else LoadNextLevel();
        }
    }
    
    private void OnLevelLoaded()
    {
        _targets = levelLoader.GetObjectsOfType<Target>();
        foreach (var target in _targets)
        {
            target.OnOccupied += OnTargetOccupied;
        }
        Movable player = levelLoader.GetObjectOfTypeWithTag<Movable>(playerTag);
        playerMovementController.SetPlayer(player);
        
        // [Echoes Mod]: 设置 GameStateRecorder 的玩家引用
        if (gameStateRecorder != null)
        {
            gameStateRecorder.SetPlayer(player);
            gameStateRecorder.Clear();
        }
        
        // [Echoes Mod]: 初始化 CharacterManager
        InitializeCharacterManager(player);
    }
    
    /// <summary>
    /// [Echoes Mod]: 初始化角色管理器
    /// </summary>
    private void InitializeCharacterManager(Movable player)
    {
        var characterManager = Echoes.Characters.CharacterManager.Instance;
        if (characterManager == null)
        {
            Debug.LogWarning("[GameManager] CharacterManager not found.");
            return;
        }
        
        // 设置玩家引用
        if (player is Player playerComponent)
        {
            characterManager.SetPlayer(playerComponent);
            
            // 应用移动速度乘区
            characterManager.OnCharacterChanged += OnCharacterChanged;
            
            // 初始化默认角色（如果尚未设置）
            if (characterManager.CurrentCharacter == null)
            {
                var allCharacters = characterManager.GetAllCharacters();
                if (allCharacters != null && allCharacters.Length > 0)
                {
                    characterManager.InitializeCharacter(allCharacters[0].CharacterId);
                }
            }
            else
            {
                // 重新应用当前角色的速度乘区
                ApplyMoveSpeedMultiplier(characterManager.CurrentCharacter);
            }
        }
    }
    
    /// <summary>
    /// [Echoes Mod]: 角色切换事件处理
    /// </summary>
    private void OnCharacterChanged(int newCharacterId, int oldCharacterId)
    {
        var characterManager = Echoes.Characters.CharacterManager.Instance;
        if (characterManager == null) return;
        
        var characterData = characterManager.GetCharacterData(newCharacterId);
        if (characterData != null)
        {
            ApplyMoveSpeedMultiplier(characterData);
        }
    }
    
    /// <summary>
    /// [Echoes Mod]: 应用移动速度乘区
    /// </summary>
    private void ApplyMoveSpeedMultiplier(Echoes.Characters.CharacterData characterData)
    {
        if (playerMovementController != null && characterData != null)
        {
            playerMovementController.SetMoveSpeedMultiplier(characterData.MoveSpeedMultiplier);
            Debug.Log($"[GameManager] Applied move speed multiplier: {characterData.MoveSpeedMultiplier}");
        }
    }

    private void EnsureSingleton(bool destroyOnLoad = true)
    {
        if (Instance == null)
        {
            Instance = this;
            if(!destroyOnLoad) DontDestroyOnLoad(gameObject);
        } 
        else Destroy(gameObject);
    }
    
    public void RestartLevel()
    {
        CommandHistoryHandler.Instance.Clear();
        
        // [Echoes Mod]: 清空状态记录
        if (gameStateRecorder != null)
        {
            gameStateRecorder.Clear();
        }
        
        // [Echoes Mod]: 重置能量
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.ResetEnergy();
        }
        
        levelLoader.RestartLevel();
    }
    
    public void LoadNextLevel()
    {
        CommandHistoryHandler.Instance.Clear();
        
        // [Echoes Mod]: 清空状态记录
        if (gameStateRecorder != null)
        {
            gameStateRecorder.Clear();
        }
        
        // [Echoes Mod]: 重置能量
        if (EnergyManager.Instance != null)
        {
            EnergyManager.Instance.ResetEnergy();
        }
        
        levelLoader.LoadNextLevel();
    }

    public void PauseGame()
    {
        if (_pauseMenuController == null || IsGamePaused) return;
        
        _pauseMenuController.Pause(false);
    }
    
    public void ResumeGame()
    {
        if (_pauseMenuController == null || !IsGamePaused) return;
        
        _pauseMenuController.Resume();
    }
}