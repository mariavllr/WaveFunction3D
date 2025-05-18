using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WFC3DMapGenerator
{
    public class WFC_Tileset_Editor : EditorWindow
    {
        // UI elements
        [SerializeField] private VisualTreeAsset m_UXML;

        // Internal variables
        [HideInInspector][SerializeField] private PreviewRenderUtility m_previewRenderUtility;
        [SerializeField] private GameObject m_selectedGameObject;
        [SerializeField] private GameObject m_previewGameObject;

        [MenuItem("Tools/WFC Generation/WFC Tileset Editor")]
        public static void ShowWindow()
        {
            WFC_Tileset_Editor window = GetWindow<WFC_Tileset_Editor>();
            window.titleContent = new GUIContent("WFC Tileset Editor");
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            m_UXML.CloneTree(root);

            m_previewRenderUtility = new PreviewRenderUtility();
            m_previewGameObject = Instantiate(m_selectedGameObject, Vector3.zero, Quaternion.identity);
            m_previewGameObject.hideFlags = HideFlags.HideAndDontSave;
            m_previewRenderUtility.AddSingleGO(m_previewGameObject);
            m_previewRenderUtility.camera.transform.position = new Vector3(0f, 0f, -10f);
            m_previewRenderUtility.camera.nearClipPlane = 5f;
		    m_previewRenderUtility.camera.farClipPlane = 20f;
        }

        private void OnFocus()
        {

        }

        private void OnGUI()
        {
            Rect rect = new Rect(0, 0, position.width, position.height);
            m_previewRenderUtility.BeginPreview(rect, previewBackground: GUIStyle.none);
            m_previewRenderUtility.Render();
            var texture = m_previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, texture);
        }

        private void OnDisable()
        {
            if (m_previewRenderUtility != null)
            {
                m_previewRenderUtility.Cleanup();
                m_previewRenderUtility = null;
            }
        }
    }
}
