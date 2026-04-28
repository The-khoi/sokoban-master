using Echoes.Characters;
using Echoes.Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: Echoes 统一输入控制器
    /// 直接在代码里读取 InputAction，不依赖 PlayerInput 的事件绑定。
    /// 挂载到场景中任意 GameObject 即可（推荐挂在 GameManager 所在物体）。
    /// </summary>
    public class EchoesInputController : MonoBehaviour
    {
        [Header("Skill")]
        [SerializeField] private TimeRewindSkill timeRewindSkill;

        // InputAction 引用（从 PlayerInput.asset 获取）
        private InputAction _timeRewindAction;
        private InputAction _enterPortalAction;
        private InputAction _returnToOuterAction;
        private InputAction _switchCharacterAction;

        private void Awake()
        {
            // [Echoes Mod]: 从 PlayerInput 获取 InputActionAsset
            var playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                var asset = playerInput.actions;
                _timeRewindAction = asset.FindAction("Player/TimeRewind");
                _enterPortalAction = asset.FindAction("Player/EnterPortal");
                _returnToOuterAction = asset.FindAction("Player/ReturnToOuter");
                _switchCharacterAction = asset.FindAction("Player/SwitchCharacter");
            }
            else
            {
                Debug.LogWarning("[EchoesInputController] PlayerInput or actions not found.");
            }
        }

        private void OnEnable()
        {
            _timeRewindAction?.Enable();
            _enterPortalAction?.Enable();
            _returnToOuterAction?.Enable();
            _switchCharacterAction?.Enable();
        }

        private void OnDisable()
        {
            _timeRewindAction?.Disable();
            _enterPortalAction?.Disable();
            _returnToOuterAction?.Disable();
            _switchCharacterAction?.Disable();
        }

        private void Update()
        {
            // [Echoes Mod]: 时间回溯
            if (_timeRewindAction != null && _timeRewindAction.WasPressedThisFrame())
            {
                if (timeRewindSkill != null)
                    timeRewindSkill.Execute();
                else
                {
                    var skill = FindObjectOfType<TimeRewindSkill>();
                    if (skill != null)
                        skill.Execute();
                }
            }

            // [Echoes Mod]: 进入传送门
            if (_enterPortalAction != null && _enterPortalAction.WasPressedThisFrame())
            {
                var portal = FindObjectOfType<PortalBox>();
                if (portal != null && portal.PlayerInRange)
                    portal.EnterPortal();
            }

            // [Echoes Mod]: 返回外层场景
            if (_returnToOuterAction != null && _returnToOuterAction.WasPressedThisFrame())
            {
                var controller = FindObjectOfType<ReturnToOuterSceneController>();
                if (controller != null && controller.CanReturn)
                    controller.ReturnToOuterScene();
            }
            
            // [Echoes Mod]: Tab 键切换角色
            if (_switchCharacterAction != null && _switchCharacterAction.WasPressedThisFrame())
            {
                SwitchToNextCharacter();
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 切换到下一个角色
        /// 严格遵守 Visual Decoupling 原则：
        /// - 不销毁 Player Root 对象
        /// - 仅替换 Visual 子物体和 Skill 组件
        /// </summary>
        private void SwitchToNextCharacter()
        {
            var characterManager = CharacterManager.Instance;
            if (characterManager == null)
            {
                Debug.LogWarning("[EchoesInputController] CharacterManager not found.");
                return;
            }
            
            var allCharacters = characterManager.GetAllCharacters();
            if (allCharacters == null || allCharacters.Length == 0)
            {
                Debug.LogWarning("[EchoesInputController] No characters available.");
                return;
            }
            
            // 找到当前角色的索引
            int currentIndex = -1;
            int currentId = characterManager.CurrentCharacterId;
            
            for (int i = 0; i < allCharacters.Length; i++)
            {
                if (allCharacters[i] != null && allCharacters[i].CharacterId == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // 计算下一个角色索引（循环）
            int nextIndex = (currentIndex + 1) % allCharacters.Length;
            
            // 切换角色
            characterManager.SwitchCharacter(allCharacters[nextIndex].CharacterId);
            
            Debug.Log($"[EchoesInputController] Switched to character: {allCharacters[nextIndex].CharacterName}");
        }
    }
}
