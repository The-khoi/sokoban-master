using UnityEngine;
using Echoes.Characters;

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

        #region Animation Helpers

        /// <summary>
        /// [Echoes Mod]: 通过 CharacterManager 获取当前 Visual 的 Animator 并触发 Bool 参数
        /// 技能子类调用此方法驱动角色动画，不直接持有 Animator 引用，保持视觉解耦
        /// </summary>
        /// <param name="paramName">Animator Bool 参数名</param>
        /// <param name="value">参数值，默认 true</param>
        protected void TriggerAnimation(string paramName, bool value = true)
        {
            if (CharacterManager.Instance == null)
            {
                Debug.LogWarning($"[Skill] TriggerAnimation: CharacterManager 未就绪，跳过动画 '{paramName}'。");
                return;
            }

            Animator animator = CharacterManager.Instance.GetCurrentAnimator();
            if (animator == null)
            {
                Debug.LogWarning($"[Skill] TriggerAnimation: 当前 Visual 上未找到 Animator，跳过动画 '{paramName}'。");
                return;
            }

            // 检查参数是否存在，避免无效参数导致的 Unity 警告
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(paramName, value);
                    Debug.Log($"[Skill] 触发动画参数：{paramName} = {value}");
                    return;
                }
            }

            Debug.LogWarning($"[Skill] TriggerAnimation: Animator 上未找到 Bool 参数 '{paramName}'，请检查 AnimatorController 配置。");
        }

        /// <summary>
        /// [Echoes Mod]: 触发 Animator Trigger 参数
        /// </summary>
        /// <param name="triggerName">Animator Trigger 参数名</param>
        protected void TriggerAnimationTrigger(string triggerName)
        {
            if (CharacterManager.Instance == null) return;

            Animator animator = CharacterManager.Instance.GetCurrentAnimator();
            if (animator == null) return;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(triggerName);
                    Debug.Log($"[Skill] 触发 Trigger：{triggerName}");
                    return;
                }
            }

            Debug.LogWarning($"[Skill] TriggerAnimationTrigger: 未找到 Trigger 参数 '{triggerName}'。");
        }

        #endregion

        #region Player Helpers

        /// <summary>
        /// [Echoes Mod]: 获取技能所挂载的 Player 组件
        /// 技能作为 Player 的子 GameObject 存在，通过 GetComponentInParent 获取
        /// </summary>
        protected Player GetPlayer()
        {
            Player player = GetComponentInParent<Player>();
            if (player == null)
            {
                Debug.LogError($"[Skill] {skillName}: 未能在父级找到 Player 组件，请确认技能已正确挂载到 Player 子对象上。");
            }
            return player;
        }

        #endregion
    }
}
