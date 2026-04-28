using System;
using UnityEngine;
using Echoes.Characters;

public class Player: Movable
{
    [Header("Animation Parameters")]
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string isMovingParameter = "IsMoving";

    // [Echoes Mod]: 视觉层挂载点
    [Header("Echoes Mod - Character System")]
    [SerializeField] private Transform visualAnchor;
    
    private bool _isAnimating = false;
    
    // [Echoes Mod]: 当前角色数据
    private CharacterData _currentCharacterData;
    
    // [Echoes Mod]: 当前 Animator 引用（从 Visual 实例获取）
    private Animator _currentAnimator;

    /// <summary>
    /// [Echoes Mod]: 视觉层挂载点
    /// </summary>
    public Transform VisualAnchor => visualAnchor;
    
    /// <summary>
    /// [Echoes Mod]: 当前角色数据
    /// </summary>
    public CharacterData CurrentCharacterData => _currentCharacterData;
    
    /// <summary>
    /// [Echoes Mod]: 当前 Animator（从 Visual 实例获取）
    /// </summary>
    public Animator CurrentAnimator => _currentAnimator;

    public override void Move(Vector3 direction, float distance, bool force = false)
    {
        base.Move(direction, distance, force);
        
        // [Echoes Mod]: 使用当前 Animator（可能为 null，由 CharacterManager 设置）
        if (_currentAnimator != null)
        {
            _currentAnimator.SetBool(isMovingParameter, false);
            _isAnimating = false;
            
            _currentAnimator.SetFloat(moveXParameter, direction.x);
            _currentAnimator.SetFloat(moveYParameter, direction.y);
            
            _currentAnimator.SetBool(isMovingParameter, true);
            _isAnimating = true;
        }
    }
    
    public void StopAnimation()
    {
        if (_isAnimating && _currentAnimator != null)
        {
            _currentAnimator.SetBool(isMovingParameter, false);
            _isAnimating = false;
        }
    }
    
    #region [Echoes Mod]: Character System Integration
    
    /// <summary>
    /// [Echoes Mod]: 设置角色数据
    /// </summary>
    public void SetCharacterData(CharacterData data)
    {
        _currentCharacterData = data;
    }
    
    /// <summary>
    /// [Echoes Mod]: 设置 Animator（由 CharacterManager 调用）
    /// </summary>
    public void SetAnimator(Animator animator)
    {
        _currentAnimator = animator;
    }
    
    /// <summary>
    /// [Echoes Mod]: 获取当前角色ID
    /// </summary>
    public int GetCurrentCharacterId()
    {
        return _currentCharacterData?.CharacterId ?? 0;
    }
    
    #endregion
}