using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 技能输入控制器
    /// 处理技能按键输入，统一使用 InputAction 回调
    /// </summary>
    public class SkillInputController : MonoBehaviour
    {
        [Header("Skill References")]
        [SerializeField] private TimeRewindSkill timeRewindSkill;
        
        /// <summary>
        /// [Echoes Mod]: 尝试执行时间回溯
        /// </summary>
        public void TryExecuteTimeRewind()
        {
            if (timeRewindSkill != null)
            {
                timeRewindSkill.Execute();
            }
            else
            {
                var skill = FindObjectOfType<TimeRewindSkill>();
                if (skill != null)
                    skill.Execute();
                else
                    Debug.LogWarning("[SkillInputController] TimeRewindSkill not found.");
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: Unity Input System 回调 - 时间回溯 (绑定 Player/TimeRewind Action)
        /// </summary>
        public void OnInputTimeRewind(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            TryExecuteTimeRewind();
        }
    }
}
