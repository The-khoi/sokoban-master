using Commands;
using Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.UI
{
    /// <summary>
    /// [Echoes Mod]: 关卡信息 HUD 控制器
    ///
    /// 显示内容：关卡名称 + 当前步数
    /// 布局位置：顶部居中
    ///
    /// 步数语义：
    /// - 执行移动 +1
    /// - 撤销 -1
    /// - 重做 +1
    /// - 关卡重载 / 时间回溯 → 归零
    ///
    /// 降级兼容：
    /// - 有 LevelData → 显示关卡名称
    /// - 无 LevelData → 显示 "Level {index+1}"
    /// </summary>
    public class LevelInfoHUDController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI stepCountText;

        [Header("Visibility")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField] private bool hideOnPause = true;

        #endregion

        #region Private Fields

        private int _stepCount;
        private bool _isPaused;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            // 监听命令事件（步数追踪）
            CommandHistoryHandler.OnCommandAdded  += OnCommandAdded;
            CommandHistoryHandler.OnCommandUndone += OnCommandUndone;
            CommandHistoryHandler.OnCommandRedone += OnCommandRedone;
            CommandHistoryHandler.OnHistoryCleared += OnHistoryCleared;

            // 监听关卡加载（更新关卡名 + 归零步数）
            var loader = FindObjectOfType<Level.LevelLoader>();
            if (loader != null)
                loader.OnLevelLoaded += OnLevelLoaded;

            RefreshAll();
        }

        private void OnDisable()
        {
            CommandHistoryHandler.OnCommandAdded   -= OnCommandAdded;
            CommandHistoryHandler.OnCommandUndone  -= OnCommandUndone;
            CommandHistoryHandler.OnCommandRedone  -= OnCommandRedone;
            CommandHistoryHandler.OnHistoryCleared -= OnHistoryCleared;

            var loader = FindObjectOfType<Level.LevelLoader>();
            if (loader != null)
                loader.OnLevelLoaded -= OnLevelLoaded;
        }

        private void Update()
        {
            if (!hideOnPause) return;

            bool paused = GameManager.Instance != null && GameManager.Instance.IsGamePaused;
            if (paused == _isPaused) return;

            _isPaused = paused;
            SetVisible(!_isPaused);
        }

        #endregion

        #region Event Handlers

        private void OnCommandAdded(Command cmd)
        {
            // 只计移动命令
            if (cmd is MoveCommand)
            {
                _stepCount++;
                UpdateStepText();
            }
        }

        private void OnCommandUndone()
        {
            if (_stepCount > 0)
            {
                _stepCount--;
                UpdateStepText();
            }
        }

        private void OnCommandRedone()
        {
            _stepCount++;
            UpdateStepText();
        }

        private void OnHistoryCleared()
        {
            // 时间回溯或关卡重载时归零
            _stepCount = 0;
            UpdateStepText();
        }

        private void OnLevelLoaded()
        {
            _stepCount = 0;
            RefreshAll();
        }

        #endregion

        #region UI Update

        private void RefreshAll()
        {
            UpdateLevelName();
            UpdateStepText();
        }

        private void UpdateLevelName()
        {
            if (levelNameText == null) return;

            var data = GameManager.Instance?.CurrentLevelData;
            if (data != null)
            {
                levelNameText.text = data.LevelName;
            }
            else
            {
                // 降级：无 LevelData 时显示 Level N
                int idx = GameManager.Instance?.CurrentLevelIndex ?? 0;
                levelNameText.text = $"Level {idx + 1}";
            }
        }

        private void UpdateStepText()
        {
            if (stepCountText == null) return;
            stepCountText.text = $"步数：{_stepCount}";
        }

        private void SetVisible(bool visible)
        {
            if (hudCanvasGroup == null) return;
            hudCanvasGroup.alpha = visible ? 1f : 0f;
            hudCanvasGroup.blocksRaycasts = false;
        }

        #endregion
    }
}
