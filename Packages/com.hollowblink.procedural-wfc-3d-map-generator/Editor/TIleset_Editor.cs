using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WFC3DMapGenerator
{
    public class WFC_Tileset_Editor : EditorWindow
    {
        // UI Elements
        [SerializeField] private VisualTreeAsset m_UXML;
        [SerializeField] private VisualElement m_previewContainer;
        [SerializeField] private IMGUIContainer m_imguiContainer;
 
        // Internal variables
        [SerializeField] private PreviewRenderUtility m_previewRenderUtility;
        [SerializeField] private GameObject m_selectedGameObject;
        [SerializeField] private GameObject m_previewGameObject;
        [SerializeField] private Vector3 m_previewGameObjectCenter;
        [SerializeField] private Vector2 m_PreviewDir = new Vector2(0, 0);

        [MenuItem("Tools/WFC Generation/WFC Tileset Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<WFC_Tileset_Editor>();
            window.titleContent = new GUIContent("WFC Tileset Editor");
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            m_UXML.CloneTree(root);

            m_previewContainer = root.Q<VisualElement>("render3DContainer");

            m_previewRenderUtility = new PreviewRenderUtility();
            m_previewRenderUtility.cameraFieldOfView = 30f;

            m_previewGameObject = Instantiate(m_selectedGameObject);
            m_previewGameObject.hideFlags = HideFlags.HideAndDontSave;
            m_previewRenderUtility.AddSingleGO(m_previewGameObject);

            m_previewGameObjectCenter = m_previewGameObject.GetComponent<Renderer>().bounds.center;
            m_previewRenderUtility.camera.clearFlags = CameraClearFlags.Color;
            m_previewRenderUtility.camera.backgroundColor = new Color(0, 0, 0, 0);
            m_previewRenderUtility.camera.nearClipPlane = 5f;
            m_previewRenderUtility.camera.farClipPlane = 20f;


            m_imguiContainer = root.Q<IMGUIContainer>("IMGUIContainer");
            m_imguiContainer.onGUIHandler = DrawPreview;
        }

        private void OnDisable()
        {
            if (m_previewRenderUtility != null)
            {
                m_previewRenderUtility.Cleanup();
                m_previewRenderUtility = null;
            }

            if (m_previewGameObject != null) DestroyImmediate(m_previewGameObject);
        }

        private void DrawPreview()
        {
            if (m_previewRenderUtility == null || m_previewGameObject == null || m_previewContainer == null) return;

            Rect rect = m_previewContainer.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            Event evt = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition) && evt.button == 0)
            {
                GUIUtility.hotControl = controlID;
                evt.Use();
            }
            else if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
            {
                m_PreviewDir -= evt.delta * 0.5f;
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                evt.Use();
            }

            Quaternion rot = Quaternion.Euler(m_PreviewDir.y, m_PreviewDir.x, 0);
            Vector3 pos = rot * (m_previewGameObjectCenter + (Vector3.back * 10f));

            m_previewRenderUtility.camera.transform.position = pos;
            m_previewRenderUtility.camera.transform.rotation = rot;

            m_previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            m_previewRenderUtility.Render(true);
            Texture resultRender = m_previewRenderUtility.EndPreview();

            GUI.DrawTexture(rect, resultRender, ScaleMode.ScaleToFit, true);
        }
    }
}