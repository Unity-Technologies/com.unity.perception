using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Perception.Randomization
{
    [CustomEditor(typeof(ScenarioBase), true)]
    class ScenarioBaseEditor : Editor
    {
        VisualElement m_ConstantsListVisualContainer;
        bool m_HasConstantsField;
        VisualElement m_InspectorPropertiesContainer;
        VisualElement m_RandomizerListPlaceholder;
        VisualElement m_Root;
        ScenarioBase m_Scenario;
        SerializedObject m_SerializedObject;

        public override VisualElement CreateInspectorGUI()
        {
            m_Scenario = (ScenarioBase)target;
            m_SerializedObject = new SerializedObject(m_Scenario);
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ScenarioBaseElement.uxml").CloneTree();

            m_RandomizerListPlaceholder = m_Root.Q<VisualElement>("randomizer-list-placeholder");

            CreatePropertyFields();
            CheckIfConstantsExist();

            var serializeConstantsButton = m_Root.Q<Button>("generate-json-config");
            serializeConstantsButton.clicked += () =>
            {
                var filePath = EditorUtility.SaveFilePanel(
                    "Generate Scenario JSON Configuration", Application.dataPath, "scenarioConfiguration", "json");
                m_Scenario.SerializeToFile(filePath);
                AssetDatabase.Refresh();
                EditorUtility.RevealInFinder(filePath);
            };

            var deserializeConstantsButton = m_Root.Q<Button>("import-json-config");
            deserializeConstantsButton.clicked += () =>
            {
                var filePath = EditorUtility.OpenFilePanel(
                    "Import Scenario JSON Configuration", Application.dataPath, "json");
                Undo.RecordObject(m_Scenario, "Deserialized scenario configuration");
                m_Scenario.DeserializeFromFile(filePath);
            };

            return m_Root;
        }

        void CreatePropertyFields()
        {
            m_InspectorPropertiesContainer = m_Root.Q<VisualElement>("inspector-properties");
            m_InspectorPropertiesContainer.Clear();

            m_ConstantsListVisualContainer = m_Root.Q<VisualElement>("constants-list");
            m_ConstantsListVisualContainer.Clear();

            var foundProperties = false;
            m_HasConstantsField = false;

            var iterator = m_SerializedObject.GetIterator();
            iterator.NextVisible(true);
            iterator.NextVisible(false);
            do
            {
                switch (iterator.name)
                {
                    case "m_Randomizers":
                        m_RandomizerListPlaceholder.Add(new RandomizerList(iterator.Copy()));
                        break;
                    case "constants":
                        m_HasConstantsField = true;
                        UIElementsEditorUtilities.CreatePropertyFields(iterator.Copy(), m_ConstantsListVisualContainer);
                        break;
                    default:
                    {
                        foundProperties = true;
                        var propertyField = UIElementsEditorUtilities.CreatePropertyField(iterator, m_Scenario.GetType());
                        m_InspectorPropertiesContainer.Add(propertyField);
                        break;
                    }
                }
            } while (iterator.NextVisible(false));

            if (!foundProperties)
                m_InspectorPropertiesContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        void CheckIfConstantsExist()
        {
            m_ConstantsListVisualContainer = m_Root.Q<VisualElement>("constants-container");
            if (!m_HasConstantsField)
            {
                m_InspectorPropertiesContainer.style.marginBottom = 0;
                m_ConstantsListVisualContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }
    }
}
