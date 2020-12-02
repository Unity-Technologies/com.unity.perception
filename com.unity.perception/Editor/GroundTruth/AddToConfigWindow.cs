using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UnityEditor.Perception.GroundTruth
{
    class AddToConfigWindow : EditorWindow
    {
        VisualElement m_Root;

        const string k_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        string m_UxmlPath;

        static List<string> s_LabelValues = new List<string>();
        static Label s_TitleLabel;
        static Label s_Status;

        public void SetStatus(string status)
        {
            s_Status.text = status;
            s_Status.style.display = DisplayStyle.Flex;
        }

        List<string> m_AllLabelConfigGuids = new List<string>();
        List<ScriptableObject> m_ConfigsContainingLabel = new List<ScriptableObject>();
        List<ScriptableObject> m_ConfigsNotContainingLabel = new List<ScriptableObject>();

        static ListView s_PresentConfigsListview;
        static ListView s_NonPresentConfigsListview;
        static ListView s_SelectedLabelsListview;
        static Label s_CurrentlyPresentTitle;
        static Label s_OtherConfigsTitle;

        List<Type> m_LabelConfigTypes = new List<Type>();
        public static void ShowWindow(string labelValue)
        {
            s_LabelValues.Clear();
            s_LabelValues.Add(labelValue);
            ShowWindow(s_LabelValues);
        }

        public static void ShowWindow(List<string> labelValues)
        {
            s_LabelValues = new List<string>(labelValues);
            var window = GetWindow<AddToConfigWindow>();

            if (s_Status != null)
            {
                s_Status.style.display = DisplayStyle.None;
            }

            if (labelValues.Count == 1)
            {

                if(s_TitleLabel != null)
                    s_TitleLabel.text = "Label: \"" + s_LabelValues.First() + "\"";

                if (s_PresentConfigsListview != null)
                {
                    s_PresentConfigsListview.style.display = DisplayStyle.Flex;
                }

                if (s_CurrentlyPresentTitle != null)
                {
                    s_CurrentlyPresentTitle.style.display = DisplayStyle.Flex;
                }

                if (s_OtherConfigsTitle != null)
                {
                    s_OtherConfigsTitle.text = "Other Label Configs in Project";
                }

                if (s_NonPresentConfigsListview != null)
                {
                    s_NonPresentConfigsListview.style.height = 150;
                }

                if (s_SelectedLabelsListview != null)
                {
                    s_SelectedLabelsListview.style.display = DisplayStyle.None;
                }

                window.titleContent = new GUIContent("Manage Label");
                window.minSize = new Vector2(400, 390);
                window.maxSize = new Vector2(700, 390);
            }
            else
            {
                if(s_TitleLabel != null)
                    s_TitleLabel.text = "Labels to Add";

                if (s_PresentConfigsListview != null)
                {
                    s_PresentConfigsListview.style.display = DisplayStyle.None;
                }

                if (s_CurrentlyPresentTitle != null)
                {
                    s_CurrentlyPresentTitle.style.display = DisplayStyle.None;
                }

                if (s_OtherConfigsTitle != null)
                {
                    s_OtherConfigsTitle.text = "All Label Configurations in Project";
                }

                if (s_NonPresentConfigsListview != null)
                {
                    s_NonPresentConfigsListview.style.height = 250;
                }

                if (s_SelectedLabelsListview != null)
                {
                    s_SelectedLabelsListview.style.display = DisplayStyle.Flex;

                }

                window.titleContent = new GUIContent("Manage Labels");
                window.minSize = new Vector2(400, 370);
                window.maxSize = new Vector2(700, 1000);
            }

            window.Init();
        }


        void Init()
        {
            Show();

            m_ConfigsContainingLabel.Clear();

            m_LabelConfigTypes = FindAllSubTypes(typeof(LabelConfig<>));

            RefreshConfigAssets();
            CheckInclusionInConfigs(m_AllLabelConfigGuids, s_LabelValues.Count == 1? s_LabelValues.First() : null);
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
            if (s_LabelValues.Count == 1)
            {
                //we are dealing with only one label

                s_PresentConfigsListview.itemsSource = m_ConfigsContainingLabel;
                VisualElement MakeItem1() => new ConfigElementLabelPresent(this, s_LabelValues.First());

                void BindItem1(VisualElement e, int i)
                {
                    if (e is ConfigElementLabelPresent element)
                    {
                        element.labelElement.text = m_ConfigsContainingLabel[i].name;
                        element.labelConfig = m_ConfigsContainingLabel[i];
                    }
                }

                s_PresentConfigsListview.itemHeight = 30;
                s_PresentConfigsListview.bindItem = BindItem1;
                s_PresentConfigsListview.makeItem = MakeItem1;
                s_PresentConfigsListview.selectionType = SelectionType.None;
            }


            //Configs not containing label
            s_NonPresentConfigsListview.itemsSource = m_ConfigsNotContainingLabel;
            VisualElement MakeItem2() => new ConfigElementLabelNotPresent(this, s_LabelValues);

            void BindItem2(VisualElement e, int i)
            {
                if (e is ConfigElementLabelNotPresent element)
                {
                    element.labelElement.text = m_ConfigsNotContainingLabel[i].name;
                    element.labelConfig = m_ConfigsNotContainingLabel[i];
                }
            }

            s_NonPresentConfigsListview.itemHeight = 30;
            s_NonPresentConfigsListview.bindItem = BindItem2;
            s_NonPresentConfigsListview.makeItem = MakeItem2;
            s_NonPresentConfigsListview.selectionType = SelectionType.None;


            //Selected labels
            s_SelectedLabelsListview.itemsSource = s_LabelValues;

            VisualElement MakeItem3() => new Label();
            void BindItem3(VisualElement e, int i)
            {
                if (e is Label label)
                {
                    label.text = s_LabelValues[i];
                    label.style.marginLeft = 2;
                    label.style.marginRight = 2;
                }
            }

            s_SelectedLabelsListview.itemHeight = 20;
            s_SelectedLabelsListview.bindItem = BindItem3;
            s_SelectedLabelsListview.makeItem = MakeItem3;
            s_SelectedLabelsListview.selectionType = SelectionType.None;
        }

        public void RefreshLists()
        {
            CheckInclusionInConfigs(m_AllLabelConfigGuids, s_LabelValues.Count == 1? s_LabelValues.First() : null);
            s_PresentConfigsListview.Refresh();
            s_NonPresentConfigsListview.Refresh();
        }
        void OnEnable()
        {
            m_UxmlPath = k_UxmlDir + "AddToConfigWindow.uxml";

            m_Root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(m_Root);

            s_TitleLabel = m_Root.Q<Label>("title");
            s_CurrentlyPresentTitle = m_Root.Q<Label>("currently-present-label");
            s_OtherConfigsTitle = m_Root.Q<Label>("other-configs-label");
            s_PresentConfigsListview = m_Root.Q<ListView>("current-configs-listview");
            s_NonPresentConfigsListview = m_Root.Q<ListView>("other-configs-listview");
            s_SelectedLabelsListview = m_Root.Q<ListView>("selected-labels-list");
            s_Status = m_Root.Q<Label>("status");
            s_Status.style.display = DisplayStyle.None;
        }

        void CheckInclusionInConfigs(List<string> configGuids, string label = null)
        {
            m_ConfigsContainingLabel.Clear();
            m_ConfigsNotContainingLabel.Clear();

            foreach (var configGuid in configGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(configGuid));
                if (label != null)
                {
                    //means we are dealing with only one label not a set
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
        const string k_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        VisualElement m_Root;
        ObjectField m_ConfigObjectField;

        public Label labelElement;
        public ScriptableObject labelConfig;

        public ConfigElementLabelPresent(AddToConfigWindow window, string targetLabel)
        {
            var uxmlPath = k_UxmlDir + "ConfigElementLabelPresent.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            labelElement = this.Q<Label>("config-name");
            var removeButton = this.Q<Button>("remove-from-config-button");
            removeButton.text = "Remove Label";

            var openButton = this.Q<Button>("open-config-button");
            openButton.clicked += () =>
            {
                Selection.SetActiveObjectWithContext(labelConfig, null);
            };

            removeButton.clicked += () =>
            {
                var editor = Editor.CreateEditor(labelConfig);
                if (editor is SemanticSegmentationLabelConfigEditor semanticEditor)
                {
                    semanticEditor.RemoveLabel(targetLabel);
                }
                else if (editor is IdLabelConfigEditor idEditor)
                {
                    idEditor.RemoveLabel(targetLabel);
                }

                window.RefreshLists();
                Object.DestroyImmediate(editor);
            };
        }
    }


    class ConfigElementLabelNotPresent : VisualElement
    {
        const string k_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        VisualElement m_Root;
        ObjectField m_ConfigObjectField;
        public Label labelElement;
        public ScriptableObject labelConfig;

        public ConfigElementLabelNotPresent(AddToConfigWindow window, List<string> targetLabels)
        {
            var uxmlPath = k_UxmlDir + "ConfigElementLabelPresent.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            labelElement = this.Q<Label>("config-name");
            var addButton = this.Q<Button>("remove-from-config-button");
            addButton.text = targetLabels.Count ==1 ? "Add Label" : "Add All Labels";

            var openButton = this.Q<Button>("open-config-button");
            openButton.clicked += () =>
            {
                Selection.SetActiveObjectWithContext(labelConfig, null);
            };

            addButton.clicked += () =>
            {
                var editor = Editor.CreateEditor(labelConfig);
                if (editor is SemanticSegmentationLabelConfigEditor semanticEditor)
                {
                    foreach (var label in targetLabels)
                    {
                        semanticEditor.AddLabel(label);
                    }
                }
                else if (editor is IdLabelConfigEditor idEditor)
                {
                    foreach (var label in targetLabels)
                    {
                        idEditor.AddLabel(label);
                    }
                }

                if (targetLabels.Count > 1)
                {
                    window.SetStatus("All Labels Added to " + labelConfig.name);
                }

                window.RefreshLists();
                Object.DestroyImmediate(editor);
            };
        }
    }
}
