using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 场景状态控制器
    /// 挂载在外层或内层场景的管理节点上，负责对该场景内所有物理对象
    /// 执行"冻结"（停止物理模拟与碰撞）和"解冻"（恢复）操作。
    ///
    /// 设计原则：
    /// - 只操作 Rigidbody2D.simulated 和 Collider2D.enabled，不触碰输入系统。
    /// - 输入层的冻结由 SceneStackManager 的 SuspendScene/ActivateScene 负责，两套机制互补。
    /// - 使用快照（Snapshot）记录冻结前的原始状态，确保解冻时精确还原。
    /// </summary>
    public class SceneStateController : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// Rigidbody2D 冻结前的 simulated 状态快照
        /// Key: Rigidbody2D 实例, Value: 冻结前的 simulated 值
        /// </summary>
        private readonly Dictionary<Rigidbody2D, bool> _rigidbodySnapshot
            = new Dictionary<Rigidbody2D, bool>();

        /// <summary>
        /// Collider2D 冻结前的 enabled 状态快照
        /// Key: Collider2D 实例, Value: 冻结前的 enabled 值
        /// </summary>
        private readonly Dictionary<Collider2D, bool> _colliderSnapshot
            = new Dictionary<Collider2D, bool>();

        /// <summary>
        /// 当前是否处于冻结状态
        /// </summary>
        private bool _isFrozen;

        #endregion

        #region Properties

        /// <summary>
        /// 当前场景是否处于冻结状态
        /// </summary>
        public bool IsFrozen => _isFrozen;

        #endregion

        #region Public API

        /// <summary>
        /// 冻结指定对象列表中的所有物理组件。
        /// 遍历列表，将 Rigidbody2D.simulated 设为 false，并禁用所有 Collider2D。
        /// 操作前会先保存原始状态快照，以便 UnfreezeScene 精确还原。
        /// </summary>
        /// <param name="levelObjects">需要冻结的关卡 GameObject 列表</param>
        public void FreezeScene(List<GameObject> levelObjects)
        {
            if (_isFrozen)
            {
                Debug.LogWarning($"[SceneStateController] '{gameObject.name}' 已处于冻结状态，跳过重复冻结。");
                return;
            }

            if (levelObjects == null || levelObjects.Count == 0)
            {
                Debug.LogWarning($"[SceneStateController] '{gameObject.name}' FreezeScene 收到空列表。");
                return;
            }

            // 清空旧快照，防止残留数据
            _rigidbodySnapshot.Clear();
            _colliderSnapshot.Clear();

            int rbCount = 0;
            int colCount = 0;

            foreach (var obj in levelObjects)
            {
                if (obj == null) continue;

                // --- 处理 Rigidbody2D ---
                // GetComponentsInChildren 包含自身，includeInactive = true 确保不遗漏
                var rigidbodies = obj.GetComponentsInChildren<Rigidbody2D>(true);
                foreach (var rb in rigidbodies)
                {
                    if (_rigidbodySnapshot.ContainsKey(rb)) continue;
                    _rigidbodySnapshot[rb] = rb.simulated;   // 保存原始值
                    rb.simulated = false;
                    rbCount++;
                }

                // --- 处理 Collider2D ---
                var colliders = obj.GetComponentsInChildren<Collider2D>(true);
                foreach (var col in colliders)
                {
                    if (_colliderSnapshot.ContainsKey(col)) continue;
                    _colliderSnapshot[col] = col.enabled;    // 保存原始值
                    col.enabled = false;
                    colCount++;
                }
            }

            _isFrozen = true;
            Debug.Log($"[SceneStateController] '{gameObject.name}' 冻结完成：" +
                      $"Rigidbody2D x{rbCount}，Collider2D x{colCount}。");
        }

        /// <summary>
        /// 解冻指定对象列表中的所有物理组件，精确还原冻结前的状态。
        /// 若某个组件在冻结时原本就是禁用的，解冻后依然保持禁用。
        /// </summary>
        /// <param name="levelObjects">需要解冻的关卡 GameObject 列表</param>
        public void UnfreezeScene(List<GameObject> levelObjects)
        {
            if (!_isFrozen)
            {
                Debug.LogWarning($"[SceneStateController] '{gameObject.name}' 未处于冻结状态，跳过解冻。");
                return;
            }

            if (levelObjects == null || levelObjects.Count == 0)
            {
                Debug.LogWarning($"[SceneStateController] '{gameObject.name}' UnfreezeScene 收到空列表。");
                return;
            }

            int rbCount = 0;
            int colCount = 0;

            foreach (var obj in levelObjects)
            {
                if (obj == null) continue;

                // --- 还原 Rigidbody2D ---
                var rigidbodies = obj.GetComponentsInChildren<Rigidbody2D>(true);
                foreach (var rb in rigidbodies)
                {
                    if (_rigidbodySnapshot.TryGetValue(rb, out bool originalSimulated))
                    {
                        rb.simulated = originalSimulated;
                        rbCount++;
                    }
                }

                // --- 还原 Collider2D ---
                var colliders = obj.GetComponentsInChildren<Collider2D>(true);
                foreach (var col in colliders)
                {
                    if (_colliderSnapshot.TryGetValue(col, out bool originalEnabled))
                    {
                        col.enabled = originalEnabled;
                        colCount++;
                    }
                }
            }

            // 清空快照，释放引用
            _rigidbodySnapshot.Clear();
            _colliderSnapshot.Clear();

            _isFrozen = false;
            Debug.Log($"[SceneStateController] '{gameObject.name}' 解冻完成：" +
                      $"Rigidbody2D x{rbCount}，Collider2D x{colCount}。");
        }

        /// <summary>
        /// 强制重置冻结状态（用于场景卸载前的清理）。
        /// 不还原物理状态，仅清空快照并重置标志位。
        /// </summary>
        public void ForceReset()
        {
            _rigidbodySnapshot.Clear();
            _colliderSnapshot.Clear();
            _isFrozen = false;
            Debug.Log($"[SceneStateController] '{gameObject.name}' 强制重置完成。");
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // 场景卸载时清理快照，防止内存泄漏
            _rigidbodySnapshot.Clear();
            _colliderSnapshot.Clear();
        }

        #endregion
    }
}
