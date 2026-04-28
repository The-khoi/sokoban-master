using System;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 能量管理器，用于技能系统
    /// 单例模式，管理玩家能量（最大3格）
    /// </summary>
    public class EnergyManager : MonoBehaviour
    {
        #region Singleton
        
        private static EnergyManager _instance;
        public static EnergyManager Instance => _instance;
        
        #endregion
        
        #region Constants
        
        /// <summary>
        /// [Echoes Mod]: 最大能量值
        /// </summary>
        private const int MaxEnergy = 3;
        
        /// <summary>
        /// [Echoes Mod]: 最小能量值
        /// </summary>
        private const int MinEnergy = 0;
        
        #endregion
        
        #region Fields
        
        private int _currentEnergy;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// [Echoes Mod]: 当能量值变化时触发
        /// 参数: (当前能量, 变化量)
        /// </summary>
        public event Action<int, int> OnEnergyChanged;
        
        /// <summary>
        /// [Echoes Mod]: 当能量已满时触发
        /// </summary>
        public event Action OnEnergyFull;
        
        /// <summary>
        /// [Echoes Mod]: 当能量耗尽时触发
        /// </summary>
        public event Action OnEnergyEmpty;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前能量值
        /// </summary>
        public int Energy => _currentEnergy;
        
        /// <summary>
        /// 最大能量值
        /// </summary>
        public int MaxEnergyValue => MaxEnergy;
        
        /// <summary>
        /// 是否有能量可用
        /// </summary>
        public bool HasEnergy => _currentEnergy > MinEnergy;
        
        /// <summary>
        /// 能量是否已满
        /// </summary>
        public bool IsFull => _currentEnergy >= MaxEnergy;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            EnsureSingleton();
            _currentEnergy = 0;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// [Echoes Mod]: 消耗能量
        /// </summary>
        /// <param name="amount">消耗数量</param>
        /// <returns>是否成功消耗</returns>
        public bool ConsumeEnergy(int amount = 1)
        {
            if (_currentEnergy < amount)
            {
                Debug.LogWarning($"[EnergyManager] Not enough energy. Current: {_currentEnergy}, Required: {amount}");
                return false;
            }
            
            int previousEnergy = _currentEnergy;
            _currentEnergy -= amount;
            
            OnEnergyChanged?.Invoke(_currentEnergy, -amount);
            
            if (_currentEnergy == MinEnergy)
            {
                OnEnergyEmpty?.Invoke();
            }
            
            Debug.Log($"[EnergyManager] Energy consumed: {amount}. Current: {_currentEnergy}/{MaxEnergy}");
            return true;
        }
        
        /// <summary>
        /// [Echoes Mod]: 增加能量
        /// </summary>
        /// <param name="amount">增加数量</param>
        public void AddEnergy(int amount = 1)
        {
            if (IsFull)
            {
                Debug.Log("[EnergyManager] Energy is already full.");
                return;
            }
            
            int previousEnergy = _currentEnergy;
            _currentEnergy = Mathf.Min(_currentEnergy + amount, MaxEnergy);
            int actualGain = _currentEnergy - previousEnergy;
            
            if (actualGain > 0)
            {
                OnEnergyChanged?.Invoke(_currentEnergy, actualGain);
                
                if (IsFull)
                {
                    OnEnergyFull?.Invoke();
                }
                
                Debug.Log($"[EnergyManager] Energy added: {actualGain}. Current: {_currentEnergy}/{MaxEnergy}");
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 重置能量
        /// </summary>
        public void ResetEnergy()
        {
            int previousEnergy = _currentEnergy;
            _currentEnergy = 0;
            
            if (previousEnergy != _currentEnergy)
            {
                OnEnergyChanged?.Invoke(_currentEnergy, -previousEnergy);
                OnEnergyEmpty?.Invoke();
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 设置能量值（用于测试或特殊效果）
        /// </summary>
        public void SetEnergy(int value)
        {
            int previousEnergy = _currentEnergy;
            _currentEnergy = Mathf.Clamp(value, MinEnergy, MaxEnergy);
            int change = _currentEnergy - previousEnergy;
            
            if (change != 0)
            {
                OnEnergyChanged?.Invoke(_currentEnergy, change);
                
                if (IsFull) OnEnergyFull?.Invoke();
                if (_currentEnergy == MinEnergy) OnEnergyEmpty?.Invoke();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void EnsureSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }
        
        #endregion
    }
}
