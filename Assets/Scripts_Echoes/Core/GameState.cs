using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 游戏状态快照，用于时间回溯技能
    /// 记录某一时刻玩家和所有箱子的完整状态
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>
        /// 玩家位置
        /// </summary>
        public Vector2 playerPosition;
        
        /// <summary>
        /// 所有箱子的状态列表
        /// </summary>
        public List<BoxState> boxStates;
        
        /// <summary>
        /// 记录时的移动步数（可选，用于UI显示）
        /// </summary>
        public int moveCount;
        
        /// <summary>
        /// 记录时的时间戳
        /// </summary>
        public float timestamp;
        
        /// <summary>
        /// [Echoes Mod]: 当前角色ID（用于角色切换回溯）
        /// </summary>
        public int characterId;
        
        public GameState()
        {
            boxStates = new List<BoxState>();
            timestamp = Time.time;
            characterId = 0;
        }
        
        /// <summary>
        /// 创建深拷贝
        /// </summary>
        public GameState Clone()
        {
            GameState clone = new GameState
            {
                playerPosition = playerPosition,
                moveCount = moveCount,
                timestamp = timestamp,
                characterId = characterId
            };
            
            clone.boxStates = new List<BoxState>();
            foreach (var boxState in boxStates)
            {
                clone.boxStates.Add(boxState.Clone());
            }
            
            return clone;
        }
    }
    
    /// <summary>
    /// [Echoes Mod]: 箱子状态快照
    /// </summary>
    [Serializable]
    public class BoxState
    {
        /// <summary>
        /// 箱子位置
        /// </summary>
        public Vector2 position;
        
        /// <summary>
        /// 是否在目标点上
        /// </summary>
        public bool isOnTarget;
        
        /// <summary>
        /// 箱子类型
        /// </summary>
        public BoxType boxType;
        
        /// <summary>
        /// 箱子实例的唯一标识（用于匹配）
        /// </summary>
        public int instanceId;
        
        public BoxState Clone()
        {
            return new BoxState
            {
                position = position,
                isOnTarget = isOnTarget,
                boxType = boxType,
                instanceId = instanceId
            };
        }
    }
    
    /// <summary>
    /// [Echoes Mod]: 箱子类型枚举
    /// </summary>
    public enum BoxType
    {
        Normal,
        Portal
    }
}
