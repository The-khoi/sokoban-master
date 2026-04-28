using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 技能基类
    /// 所有技能都应继承此类
    /// </summary>
    public abstract class Skill : MonoBehaviour
    {
        [Header("Skill Settings")]
        [SerializeField] protected string skillName;
        [SerializeField] protected int energyCost = 1;
        [SerializeField] protected bool isUnlocked = true;
        
        /// <summary>
        /// 技能名称
        /// </summary>
        public string SkillName => skillName;
        
        /// <summary>
        /// 能量消耗
        /// </summary>
        public int EnergyCost => energyCost;
        
        /// <summary>
        /// 是否已解锁
        /// </summary>
        public bool IsUnlocked => isUnlocked;
        
        /// <summary>
        /// 是否可以执行（检查能量和解锁状态）
        /// </summary>
        public virtual bool CanExecute()
        {
            if (!isUnlocked)
            {
                Debug.LogWarning($"[Skill] {skillName} is not unlocked.");
                return false;
            }
            
            if (EnergyManager.Instance == null)
            {
                Debug.LogError("[Skill] EnergyManager instance not found.");
                return false;
            }
            
            if (EnergyManager.Instance.Energy < energyCost)
            {
                Debug.LogWarning($"[Skill] Not enough energy for {skillName}. Required: {energyCost}, Current: {EnergyManager.Instance.Energy}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 执行技能
        /// </summary>
        /// <returns>是否成功执行</returns>
        public virtual bool Execute()
        {
            if (!CanExecute()) return false;
            
            // 消耗能量
            if (energyCost > 0 && EnergyManager.Instance != null)
            {
                EnergyManager.Instance.ConsumeEnergy(energyCost);
            }
            
            // 执行具体技能逻辑
            ExecuteSkill();
            
            return true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 具体技能逻辑，由子类实现
        /// </summary>
        protected abstract void ExecuteSkill();
        
        /// <summary>
        /// 解锁技能
        /// </summary>
        public virtual void Unlock()
        {
            isUnlocked = true;
        }
        
        /// <summary>
        /// 锁定技能
        /// </summary>
        public virtual void Lock()
        {
            isUnlocked = false;
        }
    }
}
