using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Unity.VisualScripting;
using GLTF.Schema;
using UnityEditor.VersionControl;

namespace WFC3DMapGenerator
{
    public class WFC_Generation_Editor : EditorWindow
    {
        // UI elements
        [SerializeField] private VisualTreeAsset m_UXML;
        [SerializeField] private RadioButtonGroup m_GenerationStrategy;
        [SerializeField] private DropdownField m_TilesetDropdown;
        [SerializeField] private DropdownField m_MapDropdown;
        [SerializeField] private VisualElement m_MapSizeSliderContainer;
        [SerializeField] private SliderInt m_MapSizeX;
        [SerializeField] private SliderInt m_MapSizeY;
        [SerializeField] private SliderInt m_MapSizeZ;
        [SerializeField] private Vector3IntField m_MapSizeField;
        [SerializeField] private VisualElement m_VisualElementWarning;
        [SerializeField] private VisualElement m_VisualElementTilesetWarning;
        [SerializeField] private ProgressBar m_ProgressBar;
        [SerializeField] private Button m_GenerateButton;

        // Internal variables
        [SerializeField] private bool m_UseChunks;
        [SerializeField] private Tileset m_Tileset;
        [SerializeField] private GameObject m_WFCPrefab;
        [SerializeField] private GameObject m_SelectedMap;
        [SerializeField] private Vector3Int m_MapSize = new Vector3Int(1, 1, 1);
        [SerializeField] private bool m_GeneratingMap = false;
        [SerializeField] private int m_GenerationProgress;

        [MenuItem("Tools/WFC Generation")]
        public static void ShowWindow()
        {
            WFC_Generation_Editor window = GetWindow<WFC_Generation_Editor>();
            window.titleContent = new GUIContent("WFC Map Generator");
        }

        private void CreateGUI()
        {
            // Load the UXML and instantiate it
            VisualElement root = rootVisualElement;
            m_UXML.CloneTree(root);
            m_GeneratingMap = false;

            // Load data for the generation strategy radio buttons (uses chunk by default)
            m_GenerationStrategy = rootVisualElement.Q<RadioButtonGroup>("strategySelector");
            if (m_GenerationStrategy != null)
            {
                m_GenerationStrategy.value = 0;
                m_UseChunks = true;
                m_GenerationStrategy.RegisterValueChangedCallback(SetGenerationStrategy);
            }

            // Load data for the tileset dropdown
            m_TilesetDropdown = rootVisualElement.Q<DropdownField>("tilesetSelector");
            if (m_TilesetDropdown != null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Tilesets")) AssetDatabase.CreateFolder("Assets/Resources", "Tilesets");
                m_TilesetDropdown.choices = Resources.LoadAll<Tileset>("Tilesets/")
                        .Select(asset => asset.name)
                        .ToList();
                if (m_TilesetDropdown.choices.Count == 0)
                {
                    m_TilesetDropdown.value = "No tilesets found";
                    m_Tileset = null;
                }
                m_TilesetDropdown.RegisterValueChangedCallback(SetTileSet);
            }

            // Load data for the map selector dropdown (new map always selected by default)
            m_MapDropdown = rootVisualElement.Q<DropdownField>("mapSelector");
            if (m_MapDropdown != null)
            {
                m_MapDropdown.choices = FindObjectsByType<WaveFunction3DGPUChunks>(FindObjectsSortMode.None)
                .Select(wfc => wfc.name)
                .ToList();
                m_MapDropdown.choices.Add("New map");
                m_MapDropdown.value = m_MapDropdown.choices[^1];
                m_MapDropdown.RegisterValueChangedCallback(SetMap);
            }

            // Load data for the map size sliders
            m_MapSizeSliderContainer = rootVisualElement.Q<VisualElement>("mapDimensionsSliderContainer");
            m_MapSizeX = rootVisualElement.Q<SliderInt>("mapDimensionsX");
            m_MapSizeY = rootVisualElement.Q<SliderInt>("mapDimensionsY");
            m_MapSizeZ = rootVisualElement.Q<SliderInt>("mapDimensionsZ");
            if (m_MapSizeX != null) m_MapSizeX.RegisterValueChangedCallback(evt => SetMapSize(evt, 'x'));
            if (m_MapSizeY != null) m_MapSizeY.RegisterValueChangedCallback(evt => SetMapSize(evt, 'y'));
            if (m_MapSizeZ != null) m_MapSizeZ.RegisterValueChangedCallback(evt => SetMapSize(evt, 'z'));
            if (m_MapSizeSliderContainer != null && !m_UseChunks) m_MapSizeSliderContainer.style.display = DisplayStyle.Flex;
            else if (m_MapSizeSliderContainer != null && m_UseChunks) m_MapSizeSliderContainer.style.display = DisplayStyle.None;

            // Load data for the map size field
            m_MapSizeField = rootVisualElement.Q<Vector3IntField>("mapDimensions");
            if (m_MapSizeField != null)
            {
                m_MapSizeField.RegisterValueChangedCallback(SetMapSize);
                if (m_UseChunks) m_MapSizeField.style.display = DisplayStyle.Flex;
                else m_MapSizeField.style.display = DisplayStyle.None;
            }

            // Load data to show or hide the warning label
            m_VisualElementWarning = rootVisualElement.Q<VisualElement>("generationWarning");
            if (m_VisualElementWarning != null)
            {
                if (m_GeneratingMap) m_VisualElementWarning.style.display = DisplayStyle.Flex;
                else m_VisualElementWarning.style.display = DisplayStyle.None;
            }

            m_VisualElementTilesetWarning = rootVisualElement.Q<VisualElement>("tilesetWarning");
            if (m_VisualElementTilesetWarning != null) m_VisualElementTilesetWarning.style.display = DisplayStyle.None;

            // Load data for the progress bar
            m_ProgressBar = rootVisualElement.Q<ProgressBar>("generationProgress");
            if (m_ProgressBar != null)
            {
                if (m_GeneratingMap)
                {
                    m_ProgressBar.style.display = DisplayStyle.Flex;
                    //TODO: Set the progress bar value
                }
                else m_ProgressBar.style.display = DisplayStyle.None;
            }

            m_GenerateButton = rootVisualElement.Q<Button>("generationButton");
            if (m_GenerateButton != null)
            {
                if (m_GeneratingMap)
                {
                    m_GenerateButton.text = "Stop generation";
                    m_GenerateButton.UnregisterCallback<ClickEvent>(StartGeneration);
                    m_GenerateButton.RegisterCallback<ClickEvent>(StopGeneration);
                }
                else
                {
                    m_GenerateButton.UnregisterCallback<ClickEvent>(StopGeneration);
                    m_GenerateButton.RegisterCallback<ClickEvent>(StartGeneration);
                }
            }
        }

        private void OnFocus()
        {
            if (m_MapDropdown != null)
            {
                m_MapDropdown.choices = FindObjectsByType<WaveFunction3DGPUChunks>(FindObjectsSortMode.None)
                    .Select(wfc => wfc.name)
                    .ToList();
                m_MapDropdown.choices.Add("New map");
                m_MapDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == "New map") m_SelectedMap = null;
                    else m_SelectedMap = FindObjectsByType<WaveFunction3DGPUChunks>(FindObjectsSortMode.None)
                                        .FirstOrDefault(wfc => wfc.name == evt.newValue)?.gameObject;
                });
            }

            if (m_TilesetDropdown != null)
            {
                m_TilesetDropdown.choices = Resources.LoadAll<Tileset>("Tilesets/")
                    .Select(asset => asset.name)
                    .ToList();
                if (m_TilesetDropdown.choices.Count == 0)
                {
                    m_TilesetDropdown.value = "No tilesets found";
                    m_Tileset = null;
                }
            }
        }

        private void OnGUI()
        {

        }

        private void SetGenerationStrategy(ChangeEvent<int> evt)
        {
            m_UseChunks = evt.newValue == 0;
            if (m_MapSizeSliderContainer != null)
            {
                if (m_UseChunks) m_MapSizeSliderContainer.style.display = DisplayStyle.None;
                else m_MapSizeSliderContainer.style.display = DisplayStyle.Flex;
            }
            if (m_MapSizeField != null)
            {
                if (m_UseChunks) m_MapSizeField.style.display = DisplayStyle.Flex;
                else m_MapSizeField.style.display = DisplayStyle.None;
            }
            Debug.Log($"Generation strategy set to: {(m_UseChunks ? "Chunks" : "Parallel")}"); // Debug log for testing
        }

        private void SetTileSet(ChangeEvent<string> evt)
        {
            m_Tileset = AssetDatabase.LoadAssetAtPath<Tileset>($"Assets/Resources/Tilesets/{evt.newValue}.asset");
            if (m_Tileset != null && m_VisualElementTilesetWarning != null) m_VisualElementTilesetWarning.style.display = DisplayStyle.None;
        }

        private void SetMap(ChangeEvent<string> evt)
        {
            if (evt.newValue == "New map") m_SelectedMap = null;
            else m_SelectedMap = FindObjectsByType<WaveFunction3DGPUChunks>(FindObjectsSortMode.None)
                                .FirstOrDefault(wfc => wfc.name == evt.newValue)?.gameObject;
        }

        private void SetMapSize(ChangeEvent<Vector3Int> evt)
        {
            m_MapSize = new Vector3Int(evt.newValue.x, evt.newValue.y, evt.newValue.z);
            if (m_MapSize.x < 1) m_MapSize.x = 1;
            if (m_MapSize.y < 1) m_MapSize.y = 1;
            if (m_MapSize.z < 1) m_MapSize.z = 1;
            if (m_MapSize != null) m_MapSizeField.value = m_MapSize;
            Debug.Log($"Map size set to: {m_MapSize}"); // Debug log for testing
        }

        private void SetMapSize(ChangeEvent<int> evt, char coordinate)
        {
            if (coordinate == 'x') m_MapSize.x = evt.newValue;
            else if (coordinate == 'y') m_MapSize.y = evt.newValue;
            else if (coordinate == 'z') m_MapSize.z = evt.newValue;
        }

        private void StartGeneration(ClickEvent evt)
        {
            if (m_Tileset == null)
            {
                if (m_VisualElementTilesetWarning != null) m_VisualElementTilesetWarning.style.display = DisplayStyle.Flex;
                return;
            }

            m_GeneratingMap = true;
            if (m_VisualElementWarning != null) m_VisualElementWarning.style.display = DisplayStyle.Flex;
            if (m_ProgressBar != null) m_ProgressBar.style.display = DisplayStyle.Flex;
            if (m_GenerateButton != null)
            {
                m_GenerateButton.text = "Stop generation";
                m_GenerateButton.UnregisterCallback<ClickEvent>(StartGeneration);
                m_GenerateButton.RegisterCallback<ClickEvent>(StopGeneration);
            }

            if (m_SelectedMap == null)
            {
                m_SelectedMap = new GameObject("WFC Map");
            }
        }

        private void StopGeneration(ClickEvent evt)
        {
            m_GeneratingMap = false;
            if (m_VisualElementWarning != null) m_VisualElementWarning.style.display = DisplayStyle.None;
            if (m_ProgressBar != null) m_ProgressBar.style.display = DisplayStyle.None;
            if (m_GenerateButton != null)
            {
                m_GenerateButton.text = "Start generation";
                m_GenerateButton.UnregisterCallback<ClickEvent>(StopGeneration);
                m_GenerateButton.RegisterCallback<ClickEvent>(StartGeneration);
            }
        }
    }
}
