using System.Collections.Generic;
using UnityEngine;

namespace Commands
{
    /// <summary>
    /// 撤销/重做历史管理器
    /// 
    /// 与时间回溯(Time Rewind)的区别：
    /// - 撤销(Undo)：单步回退，通过 CommandHistoryHandler，不消耗能量
    /// - 时间回溯(Time Rewind)：多步回退，通过 GameStateRecorder，消耗能量
    /// 
    /// 注意：执行时间回溯时会自动调用 Clear() 清空撤销历史
    /// </summary>
    public class CommandHistoryHandler
    {
        private static CommandHistoryHandler _instance;
        public static CommandHistoryHandler Instance => _instance ??= new CommandHistoryHandler();

        private readonly LinkedList<Command> _commands = new LinkedList<Command>();
        private LinkedListNode<Command> _currentCommandNode;
        private const int MaxHistorySize = 100;
        private int _currentIndex = -1;

        // [Echoes Mod]: 添加事件通知，用于状态记录
        /// <summary>
        /// [Echoes Mod]: 当命令被添加到历史时触发
        /// </summary>
        public static event System.Action<Command> OnCommandAdded;

        private CommandHistoryHandler()
        {
            _currentCommandNode = null;
        }

        public void AddCommand(Command command)
        {
            // Remove commands after the current node if we're in the middle of the history
            while (_currentCommandNode != null && _currentCommandNode.Next != null)
            {
                _commands.RemoveLast();
            }

            // Add the new command
            _commands.AddLast(command);
            _currentCommandNode = _commands.Last;
            _currentIndex++;

            // Enforce the maximum history size limit by removing the oldest command
            if (_commands.Count > MaxHistorySize)
            {
                _commands.RemoveFirst();
                _currentIndex--;
            }
            
            // [Echoes Mod]: 触发事件通知
            OnCommandAdded?.Invoke(command);
        }

        public void Undo()
        {
            if (_currentCommandNode == null || _currentIndex < 0) return;

            _currentCommandNode.Value.Undo();
            _currentCommandNode = _currentCommandNode.Previous;
            _currentIndex--;
        }

        public void Redo()
        {
            if ((_currentCommandNode == null || _currentIndex == -1) && _commands.First == null) return;

            if (_currentCommandNode == null)
                _currentCommandNode = _commands.First;
            else if (_currentCommandNode.Next != null)
                _currentCommandNode = _currentCommandNode.Next;
            else
                return;

            _currentCommandNode.Value.Redo();
            _currentIndex++;
        }

        public void Clear()
        {
            _commands.Clear();
            _currentCommandNode = null;
            _currentIndex = -1;
        }
    }
}
