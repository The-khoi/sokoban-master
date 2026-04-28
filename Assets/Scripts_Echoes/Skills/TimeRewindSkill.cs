using Commands;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 时间回溯技能
    /// 消耗1格能量，回溯到最近保存的游戏状态
    /// </summary>
    public class TimeRewindSkill : Skill
    {
        [Header("Time Rewind Settings")]
        [SerializeField] private bool playEffectOnRewind = true;
        [SerializeField] private GameObject rewindEffectPrefab;
        
        protected virtual void Awake()
        {
            skillName = "Time Rewind";
            energyCost = 1;
        }
        
        /// <summary>
        /// [Echoes Mod]: 检查是否可以执行时间回溯
        /// </summary>
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            
            // 检查是否有可回溯的状态
            if (GameStateRecorder.Instance == null)
            {
                Debug.LogError("[TimeRewindSkill] GameStateRecorder instance not found.");
                return false;
            }
            
            if (!GameStateRecorder.Instance.CanRewind)
            {
                Debug.LogWarning("[TimeRewindSkill] No state to rewind to.");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 执行时间回溯
        /// 时间回溯与撤销系统的区别：
        /// - 撤销(Undo)：单步回退，通过 CommandHistoryHandler，不消耗能量
        /// - 时间回溯(Time Rewind)：多步回退，通过 GameStateRecorder，消耗能量
        /// 执行时间回溯时需要清空撤销历史，避免状态不一致
        /// </summary>
        protected override void ExecuteSkill()
        {
            if (GameStateRecorder.Instance == null)
            {
                Debug.LogError("[TimeRewindSkill] GameStateRecorder instance not found.");
                return;
            }
            
            // 获取回溯状态
            GameState previousState = GameStateRecorder.Instance.Rewind();
            
            if (previousState == null)
            {
                Debug.LogWarning("[TimeRewindSkill] Failed to get previous state.");
                return;
            }
            
            // 恢复游戏状态
            RestoreState(previousState);
            
            // [Echoes Mod]: 清空撤销历史，避免状态不一致
            // 时间回溯已经恢复到之前的状态，撤销历史应该被清除
            CommandHistoryHandler.Instance.Clear();
            
            // 播放特效
            if (playEffectOnRewind && rewindEffectPrefab != null)
            {
                Instantiate(rewindEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Debug.Log("[TimeRewindSkill] Time rewound successfully! Command history cleared.");
        }
        
        /// <summary>
        /// [Echoes Mod]: 恢复游戏状态
        /// </summary>
        private void RestoreState(GameState state)
        {
            // 恢复玩家位置
            RestorePlayerPosition(state);
            
            // [Echoes Mod]: 恢复角色身份
            RestoreCharacter(state);
            
            // 恢复箱子位置和状态
            RestoreBoxStates(state);
            
            Debug.Log($"[TimeRewindSkill] State restored. Player pos: {state.playerPosition}, Character ID: {state.characterId}, Boxes: {state.boxStates.Count}");
        }
        
        /// <summary>
        /// [Echoes Mod]: 恢复玩家位置
        /// </summary>
        private void RestorePlayerPosition(GameState state)
        {
            // 查找玩家
            var player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.transform.position = new Vector3(
                    state.playerPosition.x,
                    state.playerPosition.y,
                    player.transform.position.z
                );
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 恢复角色身份
        /// </summary>
        private void RestoreCharacter(GameState state)
        {
            if (state.characterId <= 0) return;
            
            var characterManager = Echoes.Characters.CharacterManager.Instance;
            if (characterManager != null)
            {
                characterManager.SwitchCharacter(state.characterId);
                Debug.Log($"[TimeRewindSkill] Restored character to ID: {state.characterId}");
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 恢复箱子状态
        /// </summary>
        private void RestoreBoxStates(GameState state)
        {
            // 获取所有箱子
            var boxes = FindObjectsOfType<Movable>();
            
            foreach (var boxState in state.boxStates)
            {
                // 根据instanceId或位置匹配箱子
                foreach (var box in boxes)
                {
                    // 检查是否是箱子（有IInteractableBox组件）
                    var interactableBox = box.GetComponent<Echoes.Puzzles.IInteractableBox>();
                    if (interactableBox == null) continue;
                    
                    // 通过位置匹配（简化方案，实际可能需要更精确的匹配）
                    if (Vector2.Distance((Vector2)box.transform.position, boxState.position) < 0.1f ||
                        box.GetInstanceID() == boxState.instanceId)
                    {
                        // 恢复位置
                        box.transform.position = new Vector3(
                            boxState.position.x,
                            boxState.position.y,
                            box.transform.position.z
                        );
                        
                        // 恢复状态（触发目标点检测）
                        // 注意：实际的目标点状态由Trigger自动处理
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 快捷方法 - 尝试执行时间回溯
        /// </summary>
        public static bool TryRewind()
        {
            var instance = FindObjectOfType<TimeRewindSkill>();
            if (instance != null)
            {
                return instance.Execute();
            }
            return false;
        }
    }
}
