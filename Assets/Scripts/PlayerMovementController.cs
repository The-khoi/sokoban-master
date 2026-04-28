using System;
using Commands;
using UnityEngine;
using UnityEngine.InputSystem;
using Echoes.Characters;

public class PlayerMovementController : MonoBehaviour
{
    private Movable _player;
        
    private MoveCommand _moveUpCommand;
    private MoveCommand _moveDownCommand;
    private MoveCommand _moveLeftCommand;
    private MoveCommand _moveRightCommand;
    
    private MoveCommand _currentCommand;
    private const float CommandRepeatDelay = 0.4f;
    private const float CommandRepeatRate = 0.2f;

    // [Echoes Mod]: 移动速度乘区（影响移动频率）
    private float _moveSpeedMultiplier = 1.0f;

    public void SetPlayer(Movable player)
    {
        _player = player;
        _moveUpCommand = new MoveCommand(_player, Direction.Up, Movable.DefaultDistance);
        _moveDownCommand = new MoveCommand(_player, Direction.Down, Movable.DefaultDistance);
        _moveLeftCommand = new MoveCommand(_player, Direction.Left, Movable.DefaultDistance);
        _moveRightCommand = new MoveCommand(_player, Direction.Right, Movable.DefaultDistance);
    }

    #region [Echoes Mod]: 移动速度乘区
    
    /// <summary>
    /// [Echoes Mod]: 设置移动速度乘区
    /// 影响按键长按时的移动频率，不影响位移距离
    /// </summary>
    public void SetMoveSpeedMultiplier(float multiplier)
    {
        _moveSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3.0f);
    }
    
    /// <summary>
    /// [Echoes Mod]: 获取调整后的重复延迟
    /// </summary>
    private float GetAdjustedDelay()
    {
        return CommandRepeatDelay / _moveSpeedMultiplier;
    }
    
    /// <summary>
    /// [Echoes Mod]: 获取调整后的重复频率
    /// </summary>
    private float GetAdjustedRate()
    {
        return CommandRepeatRate / _moveSpeedMultiplier;
    }
    
    #endregion

    #region [Echoes Mod]: 提取移动逻辑为后续技能铺垫
    
    /// <summary>
    /// [Echoes Mod]: 提取移动逻辑为后续技能铺垫
    /// 尝试向指定方向移动玩家，返回是否成功执行
    /// </summary>
    /// <param name="direction">移动方向 (Vector2.up/down/left/right)</param>
    /// <returns>是否成功执行移动命令</returns>
    public bool TryMove(Vector2 direction)
    {
        if (_player == null || GameManager.Instance.IsGamePaused)
            return false;

        Direction moveDirection = Vector2ToDirection(direction);
        if (moveDirection == Direction.None)
            return false;

        MoveCommand command = new MoveCommand(_player, moveDirection, Movable.DefaultDistance);
        command.Execute();
        return true;
    }

    /// <summary>
    /// [Echoes Mod]: 辅助方法 - 将 Vector2 转换为 Direction 枚举
    /// </summary>
    private Direction Vector2ToDirection(Vector2 direction)
    {
        if (direction == Vector2.up) return Direction.Up;
        if (direction == Vector2.down) return Direction.Down;
        if (direction == Vector2.left) return Direction.Left;
        if (direction == Vector2.right) return Direction.Right;
        return Direction.None;
    }

    #endregion
    
    private void ExecuteRepeatCommand()
    {
        if (_currentCommand == null || !IsExecutionAllowed()) return;
        
        _currentCommand.Clone().Execute();
    }

    public void OnInputMoveUp(InputAction.CallbackContext context)
    {
        MoveCommand command = _moveUpCommand;
        
        UpdateRepeatingCommand(context, command);
        
        if (!IsExecutionAllowed(context)) return;
            
        command.Clone().Execute();
    }
        
    public void OnInputMoveDown(InputAction.CallbackContext context)
    {
        MoveCommand command = _moveDownCommand;
        
        UpdateRepeatingCommand(context, command);
        
        if (!IsExecutionAllowed(context)) return;
            
        command.Clone().Execute();
    }
        
    public void OnInputMoveLeft(InputAction.CallbackContext context)
    {
        MoveCommand command = _moveLeftCommand;
        
        UpdateRepeatingCommand(context, command);
        
        if (!IsExecutionAllowed(context)) return;
            
        command.Clone().Execute();
    }
        
    public void OnInputMoveRight(InputAction.CallbackContext context)
    {
        MoveCommand command = _moveRightCommand;
        
        UpdateRepeatingCommand(context, command);
        
        if (!IsExecutionAllowed(context)) return;
            
        command.Clone().Execute();
    }

    private bool IsExecutionAllowed(InputAction.CallbackContext context)
    {
        return !GameManager.Instance.IsGamePaused && context.performed;
    }
    
    private bool IsExecutionAllowed()
    {
        return !GameManager.Instance.IsGamePaused;
    }

    private void UpdateRepeatingCommand(InputAction.CallbackContext context, MoveCommand command)
    {
        if (context.started)
        {
            if (_currentCommand == null || _currentCommand == command) return;
            
            _currentCommand = null;
            CancelInvoke(nameof(ExecuteRepeatCommand));
        }
        else if (context.performed)
        {
            if (_currentCommand == command) return;
            
            if (_currentCommand != null)
                CancelInvoke(nameof(ExecuteRepeatCommand));
            
            _currentCommand = command;
            // [Echoes Mod]: 使用调整后的延迟和频率
            InvokeRepeating(nameof(ExecuteRepeatCommand), GetAdjustedDelay(), GetAdjustedRate());
        } 
        else if (context.canceled)
        {
            if (_currentCommand == null || _currentCommand != command) return;
            
            _currentCommand = null;
            CancelInvoke(nameof(ExecuteRepeatCommand));
        }
        else
        {
            if (_currentCommand == null) return;
            
            _currentCommand = null;
            CancelInvoke(nameof(ExecuteRepeatCommand));
        }
    }
}