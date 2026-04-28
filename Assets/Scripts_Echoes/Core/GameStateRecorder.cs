using System;
using System.Collections.Generic;
using System.Linq;
using Commands;
using Echoes.Puzzles;
using Level;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 游戏状态记录器，为时间回溯技能提供数据
    /// 监听玩家移动事件，记录最近3个完整游戏状态快照
    /// 
    /// 与撤销系统(CommandHistoryHandler)的区别：
    /// - 撤销(Undo)：单步回退，通过 CommandHistoryHandler，不消耗能量，最大100步
    /// - 时间回溯(Time Rewind)：多步回退，通过 GameStateRecorder，消耗能量，最大3步
    /// 
    /// 注意：执行时间回溯时会自动清空 CommandHistoryHandler，避免状态不一致
    /// </summary>
    public class GameStateRecorder : MonoBehaviour
    {
        #region Singleton
        
        private static GameStateRecorder _instance;
        public static GameStateRecorder Instance => _instance;
        
        #endregion
        
        #region Constants
        
        /// <summary>
        /// [Echoes Mod]: 最大快照深度，限制为3步
        /// </summary>
        private const int MaxSnapshotDepth = 3;
        
        #endregion
        
        #region Fields
        
        /// <summary>
        /// 状态快照栈
        /// </summary>
        private Stack<GameState> _stateStack;
        
        /// <summary>
        /// 当前移动步数计数
        /// </summary>
        private int _moveCount;
        
        /// <summary>
        /// 玩家引用
        /// </summary>
        private Movable _player;
        
        /// <summary>
        /// LevelLoader 引用
        /// </summary>
        private LevelLoader _levelLoader;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// [Echoes Mod]: 当状态被记录时触发
        /// </summary>
        public event Action<GameState> OnStateRecorded;
        
        /// <summary>
        /// [Echoes Mod]: 当执行时间回溯时触发
        /// </summary>
        public event Action<GameState> OnStateRewound;
        
        /// <summary>
        /// [Echoes Mod]: 当历史被清空时触发
        /// </summary>
        public event Action OnHistoryCleared;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前记录的状态数量
        /// </summary>
        public int StateCount => _stateStack?.Count ?? 0;
        
        /// <summary>
        /// 是否可以执行时间回溯
        /// </summary>
        public bool CanRewind => StateCount > 0;
        
        /// <summary>
        /// 当前移动步数
        /// </summary>
        public int MoveCount => _moveCount;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureSingleton();
            _stateStack = new Stack<GameState>();
        }
        
        private void OnEnable()
        {
            // [Echoes Mod]: 监听命令历史添加事件
            CommandHistoryHandler.OnCommandAdded += OnCommandAdded;
        }
        
        private void OnDisable()
        {
            // [Echoes Mod]: 取消监听
            CommandHistoryHandler.OnCommandAdded -= OnCommandAdded;
        }
        
        private void Start()
        {
            // 获取 LevelLoader 引用
            _levelLoader = FindObjectOfType<LevelLoader>();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// [Echoes Mod]: 设置玩家引用
        /// </summary>
        public void SetPlayer(Movable player)
        {
            _player = player;
        }
        
        /// <summary>
        /// [Echoes Mod]: 手动记录当前状态快照
        /// </summary>
        public void RecordSnapshot()
        {
            if (_player == null)
            {
                Debug.LogWarning("[GameStateRecorder] Player reference is null, cannot record snapshot.");
                return;
            }
            
            GameState state = CaptureCurrentState();
            
            if (state == null) return;
            
            // 限制栈深度
            while (_stateStack.Count >= MaxSnapshotDepth)
            {
                // 移除最旧的快照（需要临时栈）
                var tempStack = new Stack<GameState>();
                while (_stateStack.Count > 1)
                {
                    tempStack.Push(_stateStack.Pop());
                }
                _stateStack.Pop(); // 移除最旧的
                while (tempStack.Count > 0)
                {
                    _stateStack.Push(tempStack.Pop());
                }
            }
            
            _stateStack.Push(state);
            OnStateRecorded?.Invoke(state);
            
            Debug.Log($"[GameStateRecorder] Snapshot recorded. Stack depth: {_stateStack.Count}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 执行时间回溯，返回最近的状态快照
        /// </summary>
        /// <returns>回溯到的状态，如果无法回溯则返回 null</returns>
        public GameState Rewind()
        {
            if (!CanRewind)
            {
                Debug.LogWarning("[GameStateRecorder] No state to rewind to.");
                return null;
            }
            
            GameState state = _stateStack.Pop();
            OnStateRewound?.Invoke(state);
            
            Debug.Log($"[GameStateRecorder] Rewound to previous state. Remaining depth: {_stateStack.Count}");
            
            return state;
        }
        
        /// <summary>
        /// [Echoes Mod]: 查看最近的状态快照但不移除
        /// </summary>
        public GameState Peek()
        {
            if (!CanRewind) return null;
            return _stateStack.Peek();
        }
        
        /// <summary>
        /// [Echoes Mod]: 清空状态历史
        /// </summary>
        public void Clear()
        {
            _stateStack.Clear();
            _moveCount = 0;
            OnHistoryCleared?.Invoke();
            
            Debug.Log("[GameStateRecorder] History cleared.");
        }
        
        #endregion
        
        #region Private Methods
        
        private void EnsureSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 捕获当前游戏状态
        /// </summary>
        private GameState CaptureCurrentState()
        {
            GameState state = new GameState
            {
                playerPosition = _player.transform.position,
                moveCount = _moveCount
            };
            
            // [Echoes Mod]: 记录当前角色ID
            if (_player is Player player)
            {
                state.characterId = player.GetCurrentCharacterId();
            }
            
            // 收集所有箱子状态
            if (_levelLoader != null)
            {
                var boxes = _levelLoader.GetObjectsOfType<Movable>()
                    .Where(m => m != _player && m.GetComponent<IInteractableBox>() != null);
                
                foreach (var box in boxes)
                {
                    BoxState boxState = new BoxState
                    {
                        position = box.transform.position,
                        instanceId = box.GetInstanceID()
                    };
                    
                    // 判断箱子类型
                    if (box is PortalBox)
                    {
                        boxState.boxType = BoxType.Portal;
                        boxState.isOnTarget = ((PortalBox)box).IsOnTarget;
                    }
                    else if (box is NormalBox)
                    {
                        boxState.boxType = BoxType.Normal;
                        boxState.isOnTarget = ((NormalBox)box).IsOnTarget;
                    }
                    else
                    {
                        boxState.boxType = BoxType.Normal;
                        boxState.isOnTarget = false;
                    }
                    
                    state.boxStates.Add(boxState);
                }
            }
            
            return state;
        }
        
        /// <summary>
        /// [Echoes Mod]: 命令添加事件处理
        /// </summary>
        private void OnCommandAdded(Command command)
        {
            // 只记录移动命令
            if (command is MoveCommand)
            {
                _moveCount++;
                RecordSnapshot();
            }
        }
        
        #endregion
    }
}
