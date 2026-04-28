using System.Collections.Generic;
using Commands;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 命令历史管理器（MonoBehaviour 组件版）
    ///
    /// 与模板原有 <see cref="CommandHistoryHandler"/>（纯 C# 单例）的区别：
    /// - 本类是 MonoBehaviour，可挂载在场景中，生命周期由 Unity 管理，适合多场景递归架构。
    /// - 提供 <see cref="UndoSteps"/> 方法，供星野未来的时间回溯技能批量撤销使用。
    /// - 提供 <see cref="OnCommandExecuted"/> 事件，供 <see cref="GameStateRecorder"/> 监听。
    ///
    /// 两套系统并存策略：
    /// - 模板原有的 <see cref="CommandHistoryHandler"/> 继续处理 Undo/Redo 键盘输入（GameController）。
    /// - 本类专门对接技能系统的多步撤销需求，互不干扰。
    /// </summary>
    public class CommandHistoryManager : MonoBehaviour
    {
        #region Singleton

        private static CommandHistoryManager _instance;

        /// <summary>全局单例访问点</summary>
        public static CommandHistoryManager Instance => _instance;

        #endregion

        #region Constants

        /// <summary>
        /// 命令栈最大深度，防止无限增长
        /// </summary>
        private const int MAX_STACK_SIZE = 100;

        #endregion

        #region Fields

        /// <summary>
        /// 命令历史栈，栈顶为最近执行的命令
        /// </summary>
        private Stack<ICommand> _commandStack;

        #endregion

        #region Events

        /// <summary>
        /// [Echoes Mod]: 当命令被执行并入栈时触发。
        /// 供 <see cref="GameStateRecorder"/> 监听，记录状态快照。
        /// 参数：刚执行的命令实例
        /// </summary>
        public event System.Action<ICommand> OnCommandExecuted;

        /// <summary>
        /// [Echoes Mod]: 当命令被撤销时触发。
        /// 参数：被撤销的命令实例
        /// </summary>
        public event System.Action<ICommand> OnCommandUndone;

        /// <summary>
        /// [Echoes Mod]: 当历史被清空时触发
        /// </summary>
        public event System.Action OnHistoryCleared;

        #endregion

        #region Properties

        /// <summary>
        /// 当前栈中命令数量
        /// </summary>
        public int CommandCount => _commandStack?.Count ?? 0;

        /// <summary>
        /// 是否有可撤销的命令
        /// </summary>
        public bool CanUndo => CommandCount > 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureSingleton();
            _commandStack = new Stack<ICommand>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region Core Logic

        /// <summary>
        /// 执行一条命令并将其压入历史栈。
        /// 若栈已满，移除最旧的命令（通过临时栈翻转）。
        /// </summary>
        /// <param name="cmd">要执行的命令</param>
        public void ExecuteCommand(ICommand cmd)
        {
            if (cmd == null)
            {
                Debug.LogWarning("[CommandHistoryManager] ExecuteCommand 收到 null 命令，已跳过。");
                return;
            }

            cmd.Execute();

            // 超出上限时移除最旧的命令
            if (_commandStack.Count >= MAX_STACK_SIZE)
            {
                TrimOldestCommand();
            }

            _commandStack.Push(cmd);
            OnCommandExecuted?.Invoke(cmd);

            Debug.Log($"[CommandHistoryManager] 命令已执行并入栈。当前栈深度：{CommandCount}");
        }

        /// <summary>
        /// 撤销最近一条命令。
        /// </summary>
        public void UndoLastCommand()
        {
            if (!CanUndo)
            {
                Debug.LogWarning("[CommandHistoryManager] 没有可撤销的命令。");
                return;
            }

            ICommand cmd = _commandStack.Pop();
            cmd.Undo();
            OnCommandUndone?.Invoke(cmd);

            Debug.Log($"[CommandHistoryManager] 撤销一步。剩余栈深度：{CommandCount}");
        }

        /// <summary>
        /// 连续撤销指定步数，用于对接星野未来的时间回溯技能。
        /// 实际撤销步数取 <paramref name="stepCount"/> 与当前栈深度的较小值。
        /// </summary>
        /// <param name="stepCount">希望撤销的步数（通常为 3）</param>
        /// <returns>实际执行的撤销步数</returns>
        public int UndoSteps(int stepCount)
        {
            if (stepCount <= 0)
            {
                Debug.LogWarning($"[CommandHistoryManager] UndoSteps 收到无效步数：{stepCount}");
                return 0;
            }

            int actualSteps = Mathf.Min(stepCount, CommandCount);

            if (actualSteps == 0)
            {
                Debug.LogWarning("[CommandHistoryManager] 栈为空，无法执行多步撤销。");
                return 0;
            }

            Debug.Log($"[CommandHistoryManager] 开始多步撤销：请求 {stepCount} 步，实际执行 {actualSteps} 步。");

            for (int i = 0; i < actualSteps; i++)
            {
                UndoLastCommand();
            }

            Debug.Log($"[CommandHistoryManager] 多步撤销完成。剩余栈深度：{CommandCount}");
            return actualSteps;
        }

        /// <summary>
        /// 清空命令历史栈。
        /// 在时间回溯技能执行后调用，避免状态不一致。
        /// </summary>
        public void Clear()
        {
            _commandStack.Clear();
            OnHistoryCleared?.Invoke();
            Debug.Log("[CommandHistoryManager] 命令历史已清空。");
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 移除栈底（最旧）的命令。
        /// Stack 不支持直接访问底部，需借助临时栈翻转。
        /// </summary>
        private void TrimOldestCommand()
        {
            var tempStack = new Stack<ICommand>(_commandStack.Count - 1);

            // 将除最后一个（最旧）之外的所有命令转移到临时栈
            ICommand[] arr = _commandStack.ToArray(); // 索引 0 = 栈顶（最新）
            for (int i = 0; i < arr.Length - 1; i++)
            {
                tempStack.Push(arr[i]);
            }

            // 重建主栈（临时栈顶 = 最旧保留命令，需再次翻转）
            _commandStack.Clear();
            ICommand[] tempArr = tempStack.ToArray();
            for (int i = tempArr.Length - 1; i >= 0; i--)
            {
                _commandStack.Push(tempArr[i]);
            }

            Debug.Log("[CommandHistoryManager] 已移除最旧命令以维持栈上限。");
        }

        private void EnsureSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[CommandHistoryManager] 检测到重复实例，销毁多余组件。");
                Destroy(this);
            }
        }

        #endregion
    }
}
