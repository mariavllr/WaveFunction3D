using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Unity.VisualScripting;

public class WFC_Generation_Editor : EditorWindow
{
    // UI elements
    [SerializeField] private VisualTreeAsset m_UXML;
    [SerializeField] private RadioButtonGroup m_GenerationStrategy;
    [SerializeField] private DropdownField m_TilesetDropdown;
    [SerializeField] private DropdownField m_MapDropdown;
    [SerializeField] private Vector3Field m_MapSizeField;
    [SerializeField] private VisualElement m_VisualElementWarning;
    [SerializeField] private ProgressBar m_ProgressBar;
    [SerializeField] private Button m_GenerateButton;

    // Internal variables
    [SerializeField] private bool m_UseChunks;
    [SerializeField] private Tileset m_Tileset;
    [SerializeField] private GameObject m_WFCPrefab;
    [SerializeField] private GameObject m_SelectedMap;
    [SerializeField] private Vector3Int m_MapSize;
    [SerializeField] private bool m_GeneratingMap = false;
    [SerializeField] private int m_GenerationProgress;

    [MenuItem("Tools/WFC Generation")]
    public static void ShowWindow()
    {
        WFC_Generation_Editor window = GetWindow<WFC_Generation_Editor>();
        window.titleContent = new GUIContent("WFC Map Generatior");
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
        //TODO: Add a way to load tilesets from the project (must define the path)

        // Load data for the map selector dropdown (new map always selected by default)
        m_MapDropdown = rootVisualElement.Q<DropdownField>("mapSelector");
        if(m_MapDropdown != null)
        {
            m_MapDropdown.choices = FindObjectsByType<WaveFunction3DGPUChunks>(FindObjectsSortMode.None)
            .Select(wfc => wfc.name)
            .ToList();
            m_MapDropdown.choices.Add("New map");
            m_MapDropdown.value = m_MapDropdown.choices[^1];
        }

        // Load data for the map size field
        m_MapSizeField = rootVisualElement.Q<Vector3Field>("mapDimensions");
        m_MapSizeField?.RegisterValueChangedCallback(SetMapSize);

        // Load data to show or hide the warning label
        m_VisualElementWarning = rootVisualElement.Q<VisualElement>("generationWarning");
        if(m_VisualElementWarning != null)
        {
            if(m_GeneratingMap) m_VisualElementWarning.style.display = DisplayStyle.Flex;
            else m_VisualElementWarning.style.display = DisplayStyle.None;
        }

        // Load data for the progress bar
        m_ProgressBar = rootVisualElement.Q<ProgressBar>("generationProgress");
        if(m_ProgressBar != null)
        {
            if(m_GeneratingMap)
            {
                m_ProgressBar.style.display = DisplayStyle.Flex;
                //TODO: Set the progress bar value
            }
            else m_ProgressBar.style.display = DisplayStyle.None;
        }

        m_GenerateButton = rootVisualElement.Q<Button>("generationButton");
        if(m_GenerateButton != null)
        {
            if(m_GeneratingMap)
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
        if(m_MapDropdown != null)
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
    }

    private void OnGUI()
    {

    }

    private void SetGenerationStrategy(ChangeEvent<int> evt)
    {
        m_UseChunks = evt.newValue == 0;
        Debug.Log($"Generation strategy set to: {(m_UseChunks ? "Chunks" : "Parallel")}"); // Debug log for testing
    }

    private void SetMapSize(ChangeEvent<Vector3> evt)
    {
        m_MapSize = new Vector3Int(Mathf.RoundToInt(evt.newValue.x), Mathf.RoundToInt(evt.newValue.y), Mathf.RoundToInt(evt.newValue.z));
        if(m_MapSize.x < 1) m_MapSize.x = 1;
        if(m_MapSize.y < 1) m_MapSize.y = 1;
        if(m_MapSize.z < 1) m_MapSize.z = 1;
        if(m_MapSize != null) m_MapSizeField.value = m_MapSize;
        Debug.Log($"Map size set to: {m_MapSize}"); // Debug log for testing
    }

    private void StartGeneration(ClickEvent evt)
    {
        m_GeneratingMap = true;
        if(m_VisualElementWarning != null) m_VisualElementWarning.style.display = DisplayStyle.Flex;
        if(m_ProgressBar != null) m_ProgressBar.style.display = DisplayStyle.Flex;
        if(m_GenerateButton != null)
        {
            Debug.Log("Starting generation..."); // Debug log for testing
            m_GenerateButton.text = "Stop generation";
            m_GenerateButton.UnregisterCallback<ClickEvent>(StartGeneration);
            m_GenerateButton.RegisterCallback<ClickEvent>(StopGeneration);
        }
    }

    private void StopGeneration(ClickEvent evt)
    {
        m_GeneratingMap = false;
        if(m_VisualElementWarning != null) m_VisualElementWarning.style.display = DisplayStyle.None;
        if(m_ProgressBar != null) m_ProgressBar.style.display = DisplayStyle.None;
        if(m_GenerateButton != null)
        {
            Debug.Log("Stopping generation..."); // Debug log for testing
            m_GenerateButton.text = "Start generation";
            m_GenerateButton.UnregisterCallback<ClickEvent>(StopGeneration);
            m_GenerateButton.RegisterCallback<ClickEvent>(StartGeneration);
        }
    }
}
