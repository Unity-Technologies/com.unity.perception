using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using UnityEngine.UIElements;

namespace UnityEngine.Experimental.Perception.Randomization.Editor
{
    [CustomEditor(typeof(ScenarioBase), true)]
    class ScenarioBaseEditor : UnityEditor.Editor
    {
        ScenarioBase m_Scenario;
        SerializedObject m_SerializedObject;
        VisualElement m_Root;
        VisualElement m_InspectorPropertiesContainer;
        VisualElement m_ConstantsContainer;
        SerializedProperty m_ConstantsProperty;

        public override VisualElement CreateInspectorGUI()
        {
            m_Scenario = (ScenarioBase)target;
            m_SerializedObject = new SerializedObject(m_Scenario);
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ScenarioBaseElement.uxml").CloneTree();
            CreatePropertyFields();
            CheckIfConstantsExist();

            var serializeConstantsButton = m_Root.Query<Button>("serialize-constants").First();
            serializeConstantsButton.clicked += () => m_Scenario.Serialize();

            var deserializeConstantsButton = m_Root.Query<Button>("deserialize-constants").First();
            deserializeConstantsButton.clicked += () => m_Scenario.Deserialize();

            return m_Root;
        }

        void CreatePropertyFields()
        {
            m_InspectorPropertiesContainer = m_Root.Q<VisualElement>("inspector-properties");
            m_InspectorPropertiesContainer.Clear();

            var iterator = m_SerializedObject.GetIterator();
            var foundProperties = false;
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name == "m_Script")
                    {
                        // Skip this property
                    }
                    else if (iterator.name == "constants")
                    {
                        m_ConstantsProperty = iterator.Copy();
                    }
                    else
                    {
                        foundProperties = true;
                        var propertyField = new PropertyField(iterator.Copy());
                        propertyField.Bind(m_SerializedObject);
                        m_InspectorPropertiesContainer.Add(propertyField);
                    }
                } while (iterator.NextVisible(false));
            }

            if (!foundProperties)
                m_InspectorPropertiesContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        void CheckIfConstantsExist()
        {
            m_ConstantsContainer = m_Root.Q<VisualElement>("constants-container");
            if (m_ConstantsProperty == null)
            {
                m_InspectorPropertiesContainer.style.marginBottom = 0;
                m_ConstantsContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }
    }
}
