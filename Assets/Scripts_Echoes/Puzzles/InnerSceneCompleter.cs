using UnityEngine;
using Echoes.Core;

namespace Echoes.Puzzles
{
    /// <summary>
    /// [Echoes Mod]: 内层场景完成触发器
    /// 放置在内层场景中，当玩家收集心之碎片或到达目标点时触发
    /// 通知父层的 PortalBox 改变状态
    /// </summary>
    public class InnerSceneCompleter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int parentPortalBoxId = -1;  // 父层传送门 ID（由 SceneStackManager 自动设置）
        [SerializeField] private bool destroyOnComplete = true;
        
        [Header("Visuals")]
        [SerializeField] private GameObject completeEffect;
        [SerializeField] private AudioClip completeSound;
        
        private bool _isCompleted = false;
        
        /// <summary>
        /// 设置父层传送门 ID（由 SceneStackManager 在进入场景时调用）
        /// </summary>
        public void SetParentPortalId(int portalId)
        {
            parentPortalBoxId = portalId;
        }
        
        /// <summary>
        /// [Echoes Mod]: 触发完成（由外部调用，如收集物品、到达目标点等）
        /// </summary>
        public void Complete()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            
            // 播放特效
            if (completeEffect != null)
            {
                Instantiate(completeEffect, transform.position, Quaternion.identity);
            }
            
            // 播放音效
            if (completeSound != null)
            {
                AudioSource.PlayClipAtPoint(completeSound, transform.position);
            }
            
            // 通知父层
            if (SceneStackManager.Instance != null && parentPortalBoxId > 0)
            {
                SceneStackManager.Instance.NotifyPortalCompleted(parentPortalBoxId, true);
                Debug.Log($"[InnerSceneCompleter] Notified parent portal {parentPortalBoxId} of completion");
            }
            else
            {
                Debug.LogWarning("[InnerSceneCompleter] Cannot notify parent: SceneStackManager or portal ID missing");
            }
            
            // 销毁自身
            if (destroyOnComplete)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// [Echoes Mod]: 玩家进入触发器时自动完成
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Complete();
            }
        }
    }
}
