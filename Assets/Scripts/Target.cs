using System;
using Audio;
using Echoes.Core;
using Echoes.Puzzles;
using UnityEngine;

public class Target: MonoBehaviour
{
    // [Echoes Mod]: 保留原有 crateTag 以兼容旧预制体
    [SerializeField] private string crateTag = "Crate";
    
    public event Action OnOccupied;
    public bool IsOccupied => _isOccupied;
    
    private bool _isOccupied;

    public void OnTriggerEnter2D(Collider2D other)
    {
        // [Echoes Mod]: 优先检测 IInteractableBox 接口，兼容 NormalBox 和 PortalBox
        if (other.TryGetComponent(out IInteractableBox box))
        {
            _isOccupied = true;
            box.OnReachedTarget();
            OnOccupied?.Invoke();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.crateTargetInSfx);
            
            // [Echoes Mod]: 箱子到达目标点，增加能量
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.AddEnergy(1);
            }
        }
        // [Echoes Mod]: 兼容旧的 Tag 检测方式
        else if (other.CompareTag(crateTag))
        {
            _isOccupied = true;
            OnOccupied?.Invoke();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.crateTargetInSfx);
            
            // [Echoes Mod]: 箱子到达目标点，增加能量
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.AddEnergy(1);
            }
        }
    }
    
    public void OnTriggerExit2D(Collider2D other)
    {
        // [Echoes Mod]: 优先检测 IInteractableBox 接口
        if (other.TryGetComponent(out IInteractableBox box))
        {
            _isOccupied = false;
            box.OnLeftTarget();
            
            if (AudioManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.IsGamePaused)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.crateTargetOutSfx);
        }
        // [Echoes Mod]: 兼容旧的 Tag 检测方式
        else if (other.CompareTag(crateTag))
        {
            _isOccupied = false;
            
            if (AudioManager.Instance != null && GameManager.Instance != null && !GameManager.Instance.IsGamePaused)
                AudioManager.Instance.PlaySfx(AudioManager.Instance.crateTargetOutSfx);
        }
    }
}