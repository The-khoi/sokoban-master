using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Echoes.Characters;
using Echoes.Core;

namespace Echoes.UI
{
    /// <summary>
    /// [Echoes Mod]: 角色 HUD 控制器
    /// 
    /// 显示规则：
    /// - 仅在关卡游玩中显示（非暂停、非结算）
    /// - 监听 CharacterManager / EnergyManager 事件，不轮询
    /// - 暂停时自动隐藏，恢复时自动显示
    /// 
    /// HUD 布局（左下角）：
    /// ┌──────────────────────────────┐
    /// │ [头像]  星野未来              │
    /// │         [Q] 时间回溯         │
    /// │         撤销最近3步操作       │
    /// │         ● ● ○  (能量)        │
    /// │ [Tab] 切换角色               │
    /// └──────────────────────────────┘
    /// </summary>
    public class CharacterHUDController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Portrait")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image portraitBorder;

        [Header("Character Info")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI skillDescText;

        [Header("Skill Icon")]
        [SerializeField] private Image skillIconImage;
        [SerializeField] private TextMeshProUGUI skillCostText;

        [Header("Energy Bar")]
        [SerializeField] private Image[] energyDots;
        [SerializeField] private Color energyFullColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Color energyEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        [Header("Switch Hint")]
        [SerializeField] private TextMeshProUGUI switchHintText;

        [Header("Transition")]
        [SerializeField] private float switchAnimDuration = 0.25f;
        [SerializeField] private CanvasGroup hudCanvasGroup;

        [Header("Visibility")]
        [Tooltip("暂停时是否隐藏 HUD")]
        [SerializeField] private bool hideOnPause = true;

        [Header("Fallback Colors (无头像时使用)")]
        [SerializeField] private Color[] characterColors = new Color[]
        {
            new Color(0.4f, 0.7f, 1f),    // ID1 星野未来 - 蓝
            new Color(0.6f, 0.9f, 1f),    // ID2 凛冬静   - 冰蓝
            new Color(1f,   0.5f, 0.3f)   // ID3 焰舞红   - 橙红
        };

        #endregion

        #region Private Fields

        private Coroutine _switchAnimCoroutine;
        private bool _isPaused = false;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharacterChanged += OnCharacterChanged;

            if (EnergyManager.Instance != null)
                EnergyManager.Instance.OnEnergyChanged += OnEnergyChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharacterChanged -= OnCharacterChanged;

            if (EnergyManager.Instance != null)
                EnergyManager.Instance.OnEnergyChanged -= OnEnergyChanged;
        }

        private void Start()
        {
            RefreshAll();
        }

        private void Update()
        {
            // [Echoes Mod]: 轮询暂停状态，控制 HUD 可见性
            // 使用 GameManager 的 IsGamePaused 属性
            if (!hideOnPause) return;

            bool paused = GameManager.Instance != null && GameManager.Instance.IsGamePaused;
            if (paused != _isPaused)
            {
                _isPaused = paused;
                SetHUDVisible(!_isPaused);
            }
        }

        #endregion

        #region Visibility Control

        /// <summary>
        /// [Echoes Mod]: 控制 HUD 整体可见性
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = visible ? 1f : 0f;
                hudCanvasGroup.interactable = visible;
                hudCanvasGroup.blocksRaycasts = false; // HUD 不阻挡点击
            }
        }

        #endregion

        #region Event Handlers

        private void OnCharacterChanged(int newId, int oldId)
        {
            var data = CharacterManager.Instance?.GetCharacterData(newId);
            if (data == null) return;

            if (_switchAnimCoroutine != null)
                StopCoroutine(_switchAnimCoroutine);
            _switchAnimCoroutine = StartCoroutine(SwitchAnimation(data));
        }

        private void OnEnergyChanged(int current, int delta)
        {
            UpdateEnergyDots(current);
        }

        #endregion

        #region UI Update

        private void RefreshAll()
        {
            var manager = CharacterManager.Instance;
            if (manager == null) return;

            var data = manager.CurrentCharacter;
            if (data == null) return;

            UpdatePortrait(data);
            UpdateCharacterInfo(data);
            UpdateEnergyDots(EnergyManager.Instance?.Energy ?? 0);
            UpdateSwitchHint();
        }

        private void UpdatePortrait(CharacterData data)
        {
            if (portraitImage == null) return;

            if (data.PortraitIcon != null)
            {
                portraitImage.sprite = data.PortraitIcon;
                portraitImage.color = Color.white;
            }
            else
            {
                portraitImage.sprite = null;
                portraitImage.color = GetCharacterColor(data.CharacterId);
            }

            if (portraitBorder != null)
                portraitBorder.color = GetCharacterColor(data.CharacterId);
        }

        private void UpdateCharacterInfo(CharacterData data)
        {
            if (characterNameText != null)
                characterNameText.text = data.CharacterName;

            if (data.Skills != null && data.Skills.Length > 0)
            {
                var skill = data.Skills[0];

                if (skillNameText != null)
                    skillNameText.text = $"[Q] {GetSkillDisplayName(skill.SkillType)}";

                if (skillCostText != null)
                    skillCostText.text = skill.CustomEnergyCost >= 0
                        ? skill.CustomEnergyCost.ToString()
                        : "?";

                if (skillDescText != null)
                    skillDescText.text = GetSkillDescription(skill.SkillType);
            }
        }

        private void UpdateEnergyDots(int current)
        {
            if (energyDots == null) return;
            for (int i = 0; i < energyDots.Length; i++)
            {
                if (energyDots[i] == null) continue;
                energyDots[i].color = i < current ? energyFullColor : energyEmptyColor;
            }
        }

        private void UpdateSwitchHint()
        {
            if (switchHintText == null) return;
            int total = CharacterManager.Instance?.GetAllCharacters()?.Length ?? 0;
            switchHintText.text = total > 1 ? "[Tab] 切换角色" : "";
        }

        #endregion

        #region Animation

        private IEnumerator SwitchAnimation(CharacterData data)
        {
            if (hudCanvasGroup == null)
            {
                UpdatePortrait(data);
                UpdateCharacterInfo(data);
                yield break;
            }

            float originalAlpha = hudCanvasGroup.alpha;
            float half = switchAnimDuration * 0.5f;
            float elapsed = 0f;

            // 淡出
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                hudCanvasGroup.alpha = Mathf.Lerp(originalAlpha, 0f, elapsed / half);
                yield return null;
            }

            UpdatePortrait(data);
            UpdateCharacterInfo(data);

            // 淡入
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                hudCanvasGroup.alpha = Mathf.Lerp(0f, originalAlpha, elapsed / half);
                yield return null;
            }

            hudCanvasGroup.alpha = originalAlpha;
        }

        #endregion

        #region Helpers

        private Color GetCharacterColor(int characterId)
        {
            int index = (characterId - 1) % characterColors.Length;
            return index >= 0 ? characterColors[index] : Color.white;
        }

        private string GetSkillDisplayName(SkillType type) => type switch
        {
            SkillType.TimeRewind => "时间回溯",
            SkillType.IceFreeze  => "冰封固化",
            SkillType.BurstPush  => "爆裂推进",
            _                    => type.ToString()
        };

        private string GetSkillDescription(SkillType type) => type switch
        {
            SkillType.TimeRewind => "撤销最近3步操作",
            SkillType.IceFreeze  => "将箱子转为静态地形",
            SkillType.BurstPush  => "箱子直线滑行至撞墙",
            _                    => ""
        };

        #endregion
    }
}
