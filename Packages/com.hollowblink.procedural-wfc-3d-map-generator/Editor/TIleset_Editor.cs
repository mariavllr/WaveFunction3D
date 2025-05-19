using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace WFC3DMapGenerator
{
    public class WFC_Tileset_Editor : EditorWindow
    {
        // UI Elements

        // Preview and main
        [SerializeField] private VisualTreeAsset m_UXML;
        [SerializeField] private VisualElement m_previewContainer;
        [SerializeField] private IMGUIContainer m_imguiContainer;

        // Tileset options
        [SerializeField] private DropdownField m_tilesetDropdown;
        [SerializeField] private TextField m_tilesetNameField;
        [SerializeField] private FloatField m_tileSizeField;

        // Tile options
        [SerializeField] private DropdownField m_tileDropdown;
        [SerializeField] private TextField m_tileNameField;
        [SerializeField] private TextField m_tileTypeField;
        [SerializeField] private GameObject m_selectedGameObjectField;
        [SerializeField] private Toggle m_tileVariation90, m_tileVariation180, m_tileVariation270;

        // Transform override
        [SerializeField] private Vector3Field m_positionField, m_rotationField, m_scaleField;

        // Excluded tile types
        [SerializeField] private Foldout excludedNeighboursFrontFoldout, excludedNeighboursRightFoldout,
                                         excludedNeighboursLeftFoldout, excludedNeighboursBackFoldout;
        [SerializeField] private List<Toggle> excludedNeighboursFrontToggles, excludedNeighboursRightToggles,
                                              excludedNeighboursLeftToggles, excludedNeighboursBackToggles;

        // Socket type creation
        [SerializeField] private TextField m_socketTypeNameField;
        [SerializeField] private DropdownField m_socketTypeDropdownField;
        [SerializeField] private Button m_createSocketTypeButton, m_deleteSocketTypeButton;

        // Socket options
        [SerializeField] private DropdownField m_socketTypeDropdownFront;
        [SerializeField] private Toggle m_symetricFrontToggle, m_flippedFrontToggle;
        [SerializeField] private DropdownField m_socketTypeDropdownRight;
        [SerializeField] private Toggle m_symetricRightToggle, m_flippedRightToggle;
        [SerializeField] private DropdownField m_socketTypeDropdownLeft;
        [SerializeField] private Toggle m_symetricLeftToggle, m_flippedLeftToggle;
        [SerializeField] private DropdownField m_socketTypeDropdownBack;
        [SerializeField] private Toggle m_symetricBackToggle, m_flippedBackToggle;
        [SerializeField] private DropdownField m_socketTypeDropdownTop;
        [SerializeField] private Toggle m_rotationallyInvariantToggleTop;
        [SerializeField] private DropdownField m_socketTypeDropdownBottom;
        [SerializeField] private Toggle m_rotationallyInvariantToggleBottom;

        // Save button
        [SerializeField] private Button m_saveButton;

        //--------------------------------------------------------------------------------------------------------------------------------------

        // Internal variables

        // Preview
        [SerializeField] private PreviewRenderUtility m_previewRenderUtility;
        [SerializeField] private GameObject m_socketHelper;
        [SerializeField] private GameObject m_selectedGameObjectInstance;
        [SerializeField] private GameObject m_socketHelperInstance;
        [SerializeField] private Bounds m_selectedGameObjectInstanceBounds;
        [SerializeField] private Vector2 m_PreviewDir = new Vector2(0, 0);
        [SerializeField] private float m_PreviewDistance = 6f;

        // Tileset options
        [SerializeField] private Tileset[] m_tilesets;
        [SerializeField] private Tileset m_selectedTileset;
        [SerializeField] private string m_selectedTilesetName;
        [SerializeField] private float m_tileSize;

        // Tile options
        [SerializeField] private List<Tile3D> m_tiles;
        [SerializeField] private Tile3D m_selectedTile;
        [SerializeField] private string m_selectedTileName;
        [SerializeField] private string m_selectedTileType;
        [SerializeField] private GameObject m_selectedGameObject;
        [SerializeField] private bool rotate90, rotate180, rotate270;

        // Transform override
        [SerializeField] private Vector3 m_position, m_rotation, m_scale;

        // Excluded tile types
        [SerializeField] private List<string> m_excludedNeighboursFront, m_excludedNeighboursRight,
                                              m_excludedNeighboursLeft, m_excludedNeighboursBack;

        // Socket type creation
        [SerializeField] private string m_socketTypeName;
        [SerializeField] private List<string> m_socketTypes;

        // Socket options
        [SerializeField] private string socketTypeFront, socketTypeRight, socketTypeLeft, socketTypeBack, socketTypeTop, socketTypeBottom;
        [SerializeField] private bool m_symetricFront, m_symetricRight, m_symetricLeft, m_symetricBack;
        [SerializeField] private bool m_flippedFront, m_flippedRight, m_flippedLeft, m_flippedBack;
        [SerializeField] private bool m_rotationallyInvariantTop, m_rotationallyInvariantBottom;

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
            m_previewRenderUtility.ambientColor = Color.gray;

            m_selectedGameObjectInstance = Instantiate(m_selectedGameObject);
            m_selectedGameObjectInstance.hideFlags = HideFlags.HideAndDontSave;
            m_socketHelperInstance = Instantiate(m_socketHelper);
            m_socketHelperInstance.hideFlags = HideFlags.HideAndDontSave;
            m_selectedGameObjectInstanceBounds = GetBounds(m_selectedGameObjectInstance);
            m_socketHelperInstance.transform.position = m_selectedGameObjectInstanceBounds.center;
            m_socketHelperInstance.transform.localScale = new Vector3(2.01f, 2.01f, 2.01f); //TODO: let the user decide
            m_previewRenderUtility.AddSingleGO(m_socketHelperInstance);
            m_previewRenderUtility.AddSingleGO(m_selectedGameObjectInstance);

            m_previewRenderUtility.camera.clearFlags = CameraClearFlags.Color;
            m_previewRenderUtility.camera.backgroundColor = new Color(0, 0, 0, 0);
            m_previewRenderUtility.camera.nearClipPlane = 0.1f;
            m_previewRenderUtility.camera.farClipPlane = 25f;


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

            if (m_selectedGameObjectInstance != null) DestroyImmediate(m_selectedGameObjectInstance);
        }

        private void DrawPreview()
        {
            if (m_previewRenderUtility == null || m_selectedGameObjectInstance == null || m_previewContainer == null) return;

            Rect rect = m_imguiContainer.contentRect;
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
            else if (evt.type == EventType.ScrollWheel && rect.Contains(evt.mousePosition))
            {
                m_PreviewDistance += evt.delta.y * 0.2f; // Sensibilidad del zoom
                m_PreviewDistance = Mathf.Clamp(m_PreviewDistance, 2f, 20f); // Límites del zoom
                evt.Use();
            }

            Quaternion rot = Quaternion.Euler(m_PreviewDir.y, m_PreviewDir.x, 0);
            Vector3 pos = m_selectedGameObjectInstanceBounds.center + rot * Vector3.back * m_PreviewDistance;

            m_previewRenderUtility.camera.transform.position = pos;
            m_previewRenderUtility.camera.transform.rotation = rot;

            m_previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            m_previewRenderUtility.Render(true);
            Texture resultRender = m_previewRenderUtility.EndPreview();

            GUI.DrawTexture(rect, resultRender, ScaleMode.ScaleToFit, true);
        }

        private Bounds GetBounds(GameObject go)
        {
            List<Renderer> renderers = new List<Renderer>();
            Renderer main = go.GetComponent<Renderer>();
            if (main != null) renderers.Add(main);
            renderers.AddRange(go.GetComponentsInChildren<Renderer>());
            if (renderers.Count == 0) return new Bounds(go.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer item in renderers) bounds.Encapsulate(item.bounds);
            return bounds;
        }
    }
}