using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Echoes.Characters;
using Echoes.Core;

namespace Echoes.UI
{
    /// <summary>
    /// [Echoes Mod]: 角色状态 UI 控制器
    /// 显示当前角色的头像、颜色基调和能量槽（连续填充条）。
    ///
    /// 架构原则：
    ///   - 纯观察者模式：订阅 CharacterManager.OnCharacterChanged 和 EnergyManager.OnEnergyChanged
    ///   - 严禁在 Update 中轮询任何数据
    ///   - 平滑过渡通过协程实现，由事件触发，不占用 Update 帧
    ///
    /// 挂载说明：
    ///   将此脚本挂载到 Canvas 下的 CharacterStateUI GameObject，
    ///   在 Inspector 中关联对应的 UI 子组件引用。
    /// </summary>
    public class CharacterStateUI : MonoBehaviour
    {
        #region Inspector Fields

        [Header("── 头像区域 ──────────────────────────────")]
        [Tooltip("角色头像 Image 组件")]
        [SerializeField] private Image portraitImage;

        [Tooltip("头像边框 Image（用于颜色基调染色，可为 null）")]
        [SerializeField] private Image portraitBorderImage;

        [Tooltip("角色名称 TextMeshPro 组件（可为 null）")]
        [SerializeField] private TextMeshProUGUI characterNameText;

        [Tooltip("无头像时显示的默认精灵")]
        [SerializeField] private Sprite defaultPortraitSprite;

        [Header("── 能量槽区域 ────────────────────────────")]
        [Tooltip("能量槽填充 Image（Image Type 需设为 Filled）")]
        [SerializeField] private Image energyFillImage;

        [Tooltip("能量槽背景 Image（可为 null）")]
        [SerializeField] private Image energyBackgroundImage;

        [Tooltip("能量数值文本（格式：'2 / 3'，可为 null）")]
        [SerializeField] private TextMeshProUGUI energyValueText;

        [Header("── 颜色配置 ──────────────────────────────")]
        [Tooltip("能量槽满格颜色")]
        [SerializeField] private Color energyFullColor = new Color(0.4f, 0.85f, 1f, 1f);

        [Tooltip("能量槽空格颜色")]
        [SerializeField] private Color energyEmptyColor = new Color(0.25f, 0.25f, 0.35f, 0.8f);

        [Tooltip("无角色数据时的默认颜色基调")]
        [SerializeField] private Color defaultTintColor = Color.white;

        [Header("── 过渡动画配置 ──────────────────────────")]
        [Tooltip("能量槽填充平滑速度（值越大过渡越快）")]
        [SerializeField] private float energyLerpSpeed = 8f;

        [Tooltip("颜色基调过渡时长（秒）")]
        [SerializeField] private float colorTransitionDuration = 0.35f;

        [Tooltip("角色切换时头像的缩放脉冲倍率")]
        [SerializeField] private float portraitPulseScale = 1.15f;

        [Tooltip("头像脉冲动画时长（秒）")]
        [SerializeField] private float portraitPulseDuration = 0.25f;

        #endregion

        #region Private State

        /// <summary>能量槽当前显示的填充比例（0~1），协程的插值起点</summary>
        private float _displayedFillRatio;

        /// <summary>能量槽目标填充比例（0~1），由事件更新</summary>
        private float _targetFillRatio;

        /// <summary>当前运行中的能量平滑协程句柄</summary>
        private Coroutine _energyLerpCoroutine;

        /// <summary>当前运行中的颜色过渡协程句柄</summary>
        private Coroutine _colorTransitionCoroutine;

        /// <summary>当前运行中的头像脉冲协程句柄</summary>
        private Coroutine _portraitPulseCoroutine;

        /// <summary>头像 Image 的原始 localScale，脉冲动画还原用</summary>
        private Vector3 _portraitOriginalScale;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // 缓存头像原始缩放，防止脉冲动画累积误差
            if (portraitImage != null)
                _portraitOriginalScale = portraitImage.transform.localScale;
        }

        private void OnEnable()
        {
            SubscribeEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// [Echoes Mod]: 订阅所有数据源事件
        /// 在 OnEnable 中调用，确保每次激活都能正确监听
        /// </summary>
        private void SubscribeEvents()
        {
            // ── CharacterManager ────────────────────────────────────────────
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.OnCharacterChanged += HandleCharacterChanged;
                Debug.Log("[CharacterStateUI] 已订阅 CharacterManager.OnCharacterChanged");
            }
            else
            {
                Debug.LogWarning("[CharacterStateUI] CharacterManager 未就绪，将在 Start 中重试订阅。");
                StartCoroutine(RetrySubscribeCharacterManager());
            }

            // ── EnergyManager ────────────────────────────────────────────────
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.OnEnergyChanged += HandleEnergyChanged;
                Debug.Log("[CharacterStateUI] 已订阅 EnergyManager.OnEnergyChanged");
            }
            else
            {
                Debug.LogWarning("[CharacterStateUI] EnergyManager 未就绪，将在 Start 中重试订阅。");
                StartCoroutine(RetrySubscribeEnergyManager());
            }
        }

        /// <summary>
        /// [Echoes Mod]: 取消所有事件订阅，防止场景卸载后的空引用
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (CharacterManager.Instance != null)
                CharacterManager.Instance.OnCharacterChanged -= HandleCharacterChanged;

            if (EnergyManager.Instance != null)
                EnergyManager.Instance.OnEnergyChanged -= HandleEnergyChanged;
        }

        /// <summary>
        /// 等待 CharacterManager 就绪后重试订阅（处理初始化顺序问题）
        /// </summary>
        private IEnumerator RetrySubscribeCharacterManager()
        {
            while (CharacterManager.Instance == null)
                yield return null;

            CharacterManager.Instance.OnCharacterChanged += HandleCharacterChanged;
            Debug.Log("[CharacterStateUI] 延迟订阅 CharacterManager.OnCharacterChanged 成功。");
            RefreshCharacterDisplay(CharacterManager.Instance.CurrentCharacter);
        }

        /// <summary>
        /// 等待 EnergyManager 就绪后重试订阅
        /// </summary>
        private IEnumerator RetrySubscribeEnergyManager()
        {
            while (EnergyManager.Instance == null)
                yield return null;

            EnergyManager.Instance.OnEnergyChanged += HandleEnergyChanged;
            Debug.Log("[CharacterStateUI] 延迟订阅 EnergyManager.OnEnergyChanged 成功。");
            RefreshEnergyDisplay(EnergyManager.Instance.Energy, EnergyManager.Instance.MaxEnergyValue);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// [Echoes Mod]: 角色切换事件处理
        /// 签名：Action&lt;int newId, int oldId&gt;
        /// </summary>
        private void HandleCharacterChanged(int newCharacterId, int oldCharacterId)
        {
            CharacterData newData = CharacterManager.Instance?.GetCharacterData(newCharacterId);

            Debug.Log($"[CharacterStateUI] 角色切换：{oldCharacterId} → {newCharacterId}，" +
                      $"角色名：{newData?.CharacterName ?? "未知"}");

            RefreshCharacterDisplay(newData);
        }

        /// <summary>
        /// [Echoes Mod]: 能量变化事件处理
        /// 签名：Action&lt;int currentEnergy, int delta&gt;
        /// </summary>
        private void HandleEnergyChanged(int currentEnergy, int delta)
        {
            int maxEnergy = EnergyManager.Instance?.MaxEnergyValue ?? 3;

            Debug.Log($"[CharacterStateUI] 能量变化：{currentEnergy}/{maxEnergy}（Δ{delta:+0;-0}）");

            RefreshEnergyDisplay(currentEnergy, maxEnergy);
        }

        #endregion

        #region Refresh Methods

        /// <summary>
        /// 初始化时一次性刷新所有 UI 元素
        /// </summary>
        private void RefreshAll()
        {
            // 刷新角色显示
            CharacterData currentData = CharacterManager.Instance?.CurrentCharacter;
            RefreshCharacterDisplay(currentData);

            // 刷新能量显示（不播放过渡动画，直接设置）
            if (EnergyManager.Instance != null)
            {
                float ratio = (float)EnergyManager.Instance.Energy / EnergyManager.Instance.MaxEnergyValue;
                _displayedFillRatio = ratio;
                _targetFillRatio    = ratio;
                ApplyEnergyFill(ratio);
                UpdateEnergyText(EnergyManager.Instance.Energy, EnergyManager.Instance.MaxEnergyValue);
            }
        }

        /// <summary>
        /// 根据 CharacterData 更新头像、名称和颜色基调
        /// </summary>
        private void RefreshCharacterDisplay(CharacterData data)
        {
            // ── 头像图片 ────────────────────────────────────────────────────
            if (portraitImage != null)
            {
                portraitImage.sprite = (data?.PortraitIcon != null)
                    ? data.PortraitIcon
                    : defaultPortraitSprite;
            }

            // ── 角色名称 ────────────────────────────────────────────────────
            if (characterNameText != null)
            {
                characterNameText.text = data?.CharacterName ?? string.Empty;
            }

            // ── 颜色基调（头像边框 + 能量槽满格色）────────────────────────
            Color targetTint = (data != null)
                ? GetCharacterTintColor(data.CharacterId)
                : defaultTintColor;

            // 边框颜色过渡
            if (portraitBorderImage != null)
            {
                RestartCoroutine(ref _colorTransitionCoroutine,
                    LerpColor(portraitBorderImage, portraitBorderImage.color, targetTint, colorTransitionDuration));
            }

            // 能量槽满格色过渡
            if (energyFillImage != null)
            {
                Color targetEnergyColor = Color.Lerp(energyEmptyColor, targetTint, _targetFillRatio);
                RestartCoroutine(ref _colorTransitionCoroutine,
                    LerpColor(energyFillImage, energyFillImage.color, targetEnergyColor, colorTransitionDuration));
            }

            // ── 头像脉冲动画（角色切换反馈）────────────────────────────────
            if (portraitImage != null)
            {
                RestartCoroutine(ref _portraitPulseCoroutine,
                    PulseScale(portraitImage.transform, _portraitOriginalScale,
                               portraitPulseScale, portraitPulseDuration));
            }
        }

        /// <summary>
        /// 根据当前能量值更新能量槽（触发平滑过渡协程）
        /// </summary>
        private void RefreshEnergyDisplay(int currentEnergy, int maxEnergy)
        {
            if (maxEnergy <= 0) return;

            _targetFillRatio = Mathf.Clamp01((float)currentEnergy / maxEnergy);

            // 启动/重启平滑过渡协程
            RestartCoroutine(ref _energyLerpCoroutine, LerpEnergyFill());

            // 文本立即更新（不需要平滑）
            UpdateEnergyText(currentEnergy, maxEnergy);
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// [Echoes Mod]: 能量槽填充比例平滑过渡协程
        /// 使用 Mathf.Lerp 逐帧逼近目标值，直到误差小于阈值后停止
        /// </summary>
        private IEnumerator LerpEnergyFill()
        {
            const float SNAP_THRESHOLD = 0.001f;

            while (Mathf.Abs(_displayedFillRatio - _targetFillRatio) > SNAP_THRESHOLD)
            {
                _displayedFillRatio = Mathf.Lerp(
                    _displayedFillRatio,
                    _targetFillRatio,
                    Time.deltaTime * energyLerpSpeed
                );

                ApplyEnergyFill(_displayedFillRatio);
                yield return null;
            }

            // 精确对齐，消除浮点误差
            _displayedFillRatio = _targetFillRatio;
            ApplyEnergyFill(_displayedFillRatio);
        }

        /// <summary>
        /// [Echoes Mod]: Image 颜色平滑过渡协程
        /// </summary>
        private IEnumerator LerpColor(Image target, Color from, Color to, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.color = Color.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            target.color = to;
        }

        /// <summary>
        /// [Echoes Mod]: Transform 缩放脉冲动画协程（放大后还原）
        /// </summary>
        private IEnumerator PulseScale(Transform target, Vector3 originalScale,
                                       float pulseMultiplier, float duration)
        {
            if (target == null) yield break;

            Vector3 peakScale = originalScale * pulseMultiplier;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            // 放大阶段
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(originalScale, peakScale, elapsed / halfDuration);
                yield return null;
            }

            elapsed = 0f;

            // 还原阶段
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(peakScale, originalScale, elapsed / halfDuration);
                yield return null;
            }

            target.localScale = originalScale;
        }

        #endregion

        #region Apply Helpers

        /// <summary>
        /// 将填充比例和对应颜色写入 energyFillImage
        /// 颜色在 energyEmptyColor 和当前角色基调色之间插值
        /// </summary>
        private void ApplyEnergyFill(float ratio)
        {
            if (energyFillImage == null) return;

            energyFillImage.fillAmount = ratio;

            // 颜色随填充量在空/满之间插值
            Color characterTint = (CharacterManager.Instance?.CurrentCharacter != null)
                ? GetCharacterTintColor(CharacterManager.Instance.CurrentCharacter.CharacterId)
                : energyFullColor;

            energyFillImage.color = Color.Lerp(energyEmptyColor, characterTint, ratio);
        }

        /// <summary>
        /// 更新能量数值文本（格式：'2 / 3'）
        /// </summary>
        private void UpdateEnergyText(int current, int max)
        {
            if (energyValueText == null) return;
            energyValueText.text = $"{current} / {max}";
        }

        /// <summary>
        /// 根据角色 ID 返回对应的颜色基调
        /// 与 Visual 预制体的颜色配置保持一致
        /// </summary>
        private Color GetCharacterTintColor(int characterId)
        {
            return characterId switch
            {
                1 => new Color(0.6f, 0.8f, 1.0f, 1.0f),   // TimeKeeper  — 蓝色
                2 => new Color(0.7f, 0.9f, 1.0f, 1.0f),   // IceFreeze   — 冰蓝色
                3 => new Color(1.0f, 0.6f, 0.4f, 1.0f),   // FireDancer  — 橙红色
                _ => defaultTintColor
            };
        }

        /// <summary>
        /// 停止旧协程并启动新协程，防止多个协程同时修改同一属性
        /// </summary>
        private void RestartCoroutine(ref Coroutine handle, IEnumerator routine)
        {
            if (handle != null)
            {
                StopCoroutine(handle);
                handle = null;
            }
            handle = StartCoroutine(routine);
        }

        #endregion
    }
}
