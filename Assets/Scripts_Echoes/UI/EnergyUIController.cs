using System;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 能量UI控制器
    /// 监听 OnEnergyChanged 事件更新UI，不在 Update 中轮询
    /// </summary>
    public class EnergyUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image[] energyIcons;
        [SerializeField] private Sprite fullEnergySprite;
        [SerializeField] private Sprite emptyEnergySprite;
        [SerializeField] private Color fullColor = Color.white;
        [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        [Header("Animation")]
        [SerializeField] private bool useAnimation = true;
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseDuration = 0.2f;
        
        private int _currentEnergy;
        
        private void OnEnable()
        {
            // [Echoes Mod]: 监听能量变化事件
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.OnEnergyChanged += OnEnergyChanged;
                EnergyManager.Instance.OnEnergyFull += OnEnergyFull;
                EnergyManager.Instance.OnEnergyEmpty += OnEnergyEmpty;
                
                // 初始化UI
                UpdateUI(EnergyManager.Instance.Energy);
            }
        }
        
        private void OnDisable()
        {
            // [Echoes Mod]: 取消监听
            if (EnergyManager.Instance != null)
            {
                EnergyManager.Instance.OnEnergyChanged -= OnEnergyChanged;
                EnergyManager.Instance.OnEnergyFull -= OnEnergyFull;
                EnergyManager.Instance.OnEnergyEmpty -= OnEnergyEmpty;
            }
        }
        
        private void Start()
        {
            // 再次确保初始化
            if (EnergyManager.Instance != null)
            {
                UpdateUI(EnergyManager.Instance.Energy);
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 能量变化事件处理
        /// </summary>
        private void OnEnergyChanged(int currentEnergy, int change)
        {
            UpdateUI(currentEnergy);
            
            // 播放动画
            if (useAnimation && change != 0)
            {
                int iconIndex = change > 0 ? currentEnergy - 1 : currentEnergy;
                if (iconIndex >= 0 && iconIndex < energyIcons.Length)
                {
                    StartCoroutine(PulseAnimation(energyIcons[iconIndex].transform));
                }
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 能量已满事件处理
        /// </summary>
        private void OnEnergyFull()
        {
            // 可以在这里播放满能量特效
            Debug.Log("[EnergyUIController] Energy is full!");
        }
        
        /// <summary>
        /// [Echoes Mod]: 能量耗尽事件处理
        /// </summary>
        private void OnEnergyEmpty()
        {
            // 可以在这里播放能量耗尽提示
            Debug.Log("[EnergyUIController] Energy depleted!");
        }
        
        /// <summary>
        /// [Echoes Mod]: 更新UI显示
        /// </summary>
        private void UpdateUI(int currentEnergy)
        {
            _currentEnergy = currentEnergy;
            
            if (energyIcons == null || energyIcons.Length == 0) return;
            
            for (int i = 0; i < energyIcons.Length; i++)
            {
                if (energyIcons[i] == null) continue;
                
                bool isFull = i < currentEnergy;
                
                // 更新精灵图
                if (fullEnergySprite != null && emptyEnergySprite != null)
                {
                    energyIcons[i].sprite = isFull ? fullEnergySprite : emptyEnergySprite;
                }
                
                // 更新颜色
                energyIcons[i].color = isFull ? fullColor : emptyColor;
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 脉冲动画
        /// </summary>
        private System.Collections.IEnumerator PulseAnimation(Transform target)
        {
            if (target == null) yield break;
            
            Vector3 originalScale = target.localScale;
            Vector3 targetScale = originalScale * pulseScale;
            
            float elapsed = 0f;
            
            // 放大
            while (elapsed < pulseDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2f);
                target.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            elapsed = 0f;
            
            // 缩小
            while (elapsed < pulseDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2f);
                target.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            target.localScale = originalScale;
        }
    }
}
