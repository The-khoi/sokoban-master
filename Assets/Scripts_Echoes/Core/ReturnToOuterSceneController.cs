using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Core
{
    /// <summary>
    /// [Echoes Mod]: 返回外层场景控制器
    /// 放置在内层场景中，用于返回外层
    /// </summary>
    public class ReturnToOuterSceneController : MonoBehaviour
    {
        [Header("Return Settings")]
        [SerializeField] private bool showReturnPrompt = true;
        [SerializeField] private string returnPromptText = "Press Backspace to return";
        
        /// <summary>
        /// [Echoes Mod]: 是否可以返回
        /// </summary>
        public bool CanReturn => SceneStackManager.Instance != null && SceneStackManager.Instance.HasInnerScene;
        
        /// <summary>
        /// [Echoes Mod]: 返回外层场景
        /// </summary>
        public void ReturnToOuterScene()
        {
            if (!CanReturn)
            {
                Debug.LogWarning("[ReturnToOuterSceneController] Cannot return to outer scene.");
                return;
            }
            
            Debug.Log("[ReturnToOuterSceneController] Returning to outer scene...");
            SceneStackManager.Instance.ReturnToOuterScene();
        }
        
        /// <summary>
        /// [Echoes Mod]: Unity Input System 回调 - 返回外层场景 (绑定 Player/ReturnToOuter Action)
        /// </summary>
        public void OnInputReturnToOuter(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (!CanReturn) return;
            
            ReturnToOuterScene();
        }
        
        #region UI (Optional)
        
        private void OnGUI()
        {
            if (!showReturnPrompt || !CanReturn) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            Rect rect = new Rect(Screen.width / 2 - 100, Screen.height - 50, 200, 30);
            GUI.Label(rect, returnPromptText, style);
        }
        
        #endregion
    }
}
