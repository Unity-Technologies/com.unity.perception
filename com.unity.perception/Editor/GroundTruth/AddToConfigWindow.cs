using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering.UI;
using UnityEngine.UIElements;
using Object = System.Object;

namespace UnityEditor.Perception.GroundTruth
{
    class AddToConfigWindow : EditorWindow
    {
        private VisualElement m_Root;

        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;

        private static string m_LabelValue;
        private static Label m_TitleLabel;

        private List<string> m_AllLabelConfigGuids = new List<string>();
        private List<ScriptableObject> m_ConfigsContainingLabel = new List<ScriptableObject>();
        private List<ScriptableObject> m_ConfigsNotContainingLabel = new List<ScriptableObject>();

        private ListView m_PresentConfigsListview;
        private ListView m_NonPresentConfigsListview;

        private List<Type> m_LabelConfigTypes = new List<Type>();
        public static void ShowWindow(string labelValue)
        {
            m_LabelValue = labelValue;
            var window = GetWindow<AddToConfigWindow>();
            window.minSize = new Vector2(350, 385);
            window.maxSize = new Vector2(350, 385);
            window.titleContent = new GUIContent("Manage Label");
            window.Init();
            window.Show();
        }

        void Init()
        {
            m_ConfigsContainingLabel.Clear();

            if(m_TitleLabel != null)
                m_TitleLabel.text = "Label: \"" + m_LabelValue + "\"";

            m_LabelConfigTypes = FindAllSubTypes(typeof(LabelConfig<>));

            RefreshConfigAssets();
            CheckInclusionInConfigs(m_AllLabelConfigGuids, m_LabelConfigTypes, m_LabelValue, this);
            SetupListViews();
        }

        void RefreshConfigAssets()
        {
            AssetDatabase.Refresh();

            m_AllLabelConfigGuids.Clear();
            foreach (var type in m_LabelConfigTypes)
            {
                m_AllLabelConfigGuids.AddRange(AssetDatabase.FindAssets("t:"+type.Name));
            }
        }

        void SetupListViews()
        {
            //configs containing label
            m_PresentConfigsListview.itemsSource = m_ConfigsContainingLabel;
            VisualElement MakeItem1() => new ConfigElementLabelPresent(this, m_LabelValue);

            void BindItem1(VisualElement e, int i)
            {
                if (e is ConfigElementLabelPresent element)
                {
                    element.m_Label.text = m_ConfigsContainingLabel[i].name;
                    element.m_LabelConfig = m_ConfigsContainingLabel[i];
                }
            }

            m_PresentConfigsListview.itemHeight = 30;
            m_PresentConfigsListview.bindItem = BindItem1;
            m_PresentConfigsListview.makeItem = MakeItem1;
            m_PresentConfigsListview.selectionType = SelectionType.None;

            //Configs not containing label
            m_NonPresentConfigsListview.itemsSource = m_ConfigsNotContainingLabel;
            VisualElement MakeItem2() => new ConfigElementLabelNotPresent(this, m_LabelValue);

            void BindItem2(VisualElement e, int i)
            {
                if (e is ConfigElementLabelNotPresent element)
                {
                    element.m_Label.text = m_ConfigsNotContainingLabel[i].name;
                    element.m_LabelConfig = m_ConfigsNotContainingLabel[i];
                }
            }

            m_NonPresentConfigsListview.itemHeight = 30;
            m_NonPresentConfigsListview.bindItem = BindItem2;
            m_NonPresentConfigsListview.makeItem = MakeItem2;
            m_NonPresentConfigsListview.selectionType = SelectionType.None;
        }

        public void RefreshLists()
        {
            CheckInclusionInConfigs(m_AllLabelConfigGuids, m_LabelConfigTypes, m_LabelValue, this);
            m_PresentConfigsListview.Refresh();
            m_NonPresentConfigsListview.Refresh();
        }
        void OnEnable()
        {
            m_UxmlPath = m_UxmlDir + "AddToConfigWindow.uxml";

            m_Root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(m_Root);

            m_TitleLabel = m_Root.Query<Label>("title");
            m_TitleLabel.text = "Add \"" + m_LabelValue + "\" to Label Configurations";

            m_PresentConfigsListview = m_Root.Query<ListView>("current-configs-listview");
            m_NonPresentConfigsListview = m_Root.Query<ListView>("other-configs-listview");
        }

        void CheckInclusionInConfigs(List<string> configGuids, List<Type> configTypes, string label, AddToConfigWindow window)
        {
            m_ConfigsContainingLabel.Clear();
            m_ConfigsNotContainingLabel.Clear();

            foreach (var configGuid in configGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(configGuid));
                var methodInfo = asset.GetType().GetMethod(IdLabelConfig.DoesLabelMatchAnEntryName);

                if (methodInfo == null)
                    continue;

                object[] parametersArray = new object[1];
                parametersArray[0] = label;

                var labelExistsInConfig = (bool) methodInfo.Invoke(asset, parametersArray);

                if (labelExistsInConfig)
                {
                    m_ConfigsContainingLabel.Add(asset);
                }
                else
                {
                    m_ConfigsNotContainingLabel.Add(asset);
                }
            }
        }

        public static List<Type> FindAllSubTypes(Type superType)
        {
            Assembly assembly = Assembly.GetAssembly(superType);
            Type[] types = assembly.GetTypes();
            List<Type> subclasses = types.Where(t => IsSubclassOfRawGeneric(superType, t)).ToList();
            return subclasses;


            bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
                while (toCheck != null && toCheck != typeof(object)) {
                    var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    if (generic == cur) {
                        return true;
                    }
                    toCheck = toCheck.BaseType;
                }
                return false;
            }
        }
    }

    class ConfigElementLabelPresent : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private VisualElement m_Root;
        private Button m_RemoveButton;
        private ObjectField m_ConfigObjectField;

        public Label m_Label;
        public ScriptableObject m_LabelConfig;

        public ConfigElementLabelPresent(AddToConfigWindow window, string targetLabel)
        {
            m_UxmlPath = m_UxmlDir + "ConfigElementLabelPresent.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("config-name");
            m_RemoveButton = this.Q<Button>("remove-from-config-button");
            m_RemoveButton.text = "Remove Label";


            m_RemoveButton.clicked += () =>
            {
                var methodInfo = m_LabelConfig.GetType().GetMethod(IdLabelConfig.RemoveLabelName);
                if (methodInfo != null)
                {
                    object[] parametersArray = new object[1];
                    parametersArray[0] = targetLabel;

                    methodInfo.Invoke(m_LabelConfig, parametersArray);
                    EditorUtility.SetDirty(m_LabelConfig);
                    window.RefreshLists();
                }
            };
        }
    }


    class ConfigElementLabelNotPresent : VisualElement
    {
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;
        private VisualElement m_Root;
        private Button m_AddButton;
        private ObjectField m_ConfigObjectField;
        public Label m_Label;
        public ScriptableObject m_LabelConfig;

        public ConfigElementLabelNotPresent(AddToConfigWindow window, string targetLabel)
        {
            m_UxmlPath = m_UxmlDir + "ConfigElementLabelPresent.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("config-name");
            m_AddButton = this.Q<Button>("remove-from-config-button");
            m_AddButton.text = "Add Label";

            m_AddButton.clicked += () =>
            {
                var methodInfo = m_LabelConfig.GetType().GetMethod(IdLabelConfig.AddLabelName);
                if (methodInfo != null)
                {
                    object[] parametersArray = new object[1];
                    parametersArray[0] = targetLabel;

                    methodInfo.Invoke(m_LabelConfig, parametersArray);
                    EditorUtility.SetDirty(m_LabelConfig);
                    window.RefreshLists();
                }
            };
        }
    }
}
