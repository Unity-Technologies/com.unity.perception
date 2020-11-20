using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    class AddToConfigWindow : EditorWindow
    {
        private VisualElement m_Root;

        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private string m_UxmlPath;

        private static HashSet<string> m_LabelValues = new HashSet<string>();
        private static Label m_TitleLabel;
        private static Label m_Status;

        public void SetStatus(string status)
        {
            m_Status.text = status;
            m_Status.style.display = DisplayStyle.Flex;
        }

        private List<string> m_AllLabelConfigGuids = new List<string>();
        private List<ScriptableObject> m_ConfigsContainingLabel = new List<ScriptableObject>();
        private List<ScriptableObject> m_ConfigsNotContainingLabel = new List<ScriptableObject>();

        private static ListView m_PresentConfigsListview;
        private static ListView m_NonPresentConfigsListview;
        private static Label m_CurrentlyPresentTitle;
        private static Label m_OtherConfigsTitle;

        private List<Type> m_LabelConfigTypes = new List<Type>();
        public static void ShowWindow(string labelValue)
        {
            m_LabelValues.Clear();
            m_LabelValues.Add(labelValue);
            ShowWindow(m_LabelValues);
        }

        public static void ShowWindow(HashSet<string> labelValues)
        {
            m_LabelValues = new HashSet<string>(labelValues);
            var window = GetWindow<AddToConfigWindow>();
            if (labelValues.Count == 1)
            {

                if(m_TitleLabel != null)
                    m_TitleLabel.text = "Label: \"" + m_LabelValues.First() + "\"";

                if (m_PresentConfigsListview != null)
                {
                    m_PresentConfigsListview.style.display = DisplayStyle.Flex;
                }

                if (m_CurrentlyPresentTitle != null)
                {
                    m_CurrentlyPresentTitle.style.display = DisplayStyle.Flex;
                }

                if (m_OtherConfigsTitle != null)
                {
                    m_OtherConfigsTitle.text = "Other Label Configs in Project:";
                }

                if (m_NonPresentConfigsListview != null)
                {
                    m_NonPresentConfigsListview.style.height = 150;
                }


                window.titleContent = new GUIContent("Manage Label");
                window.minSize = new Vector2(350, 385);
                window.maxSize = new Vector2(350, 600);
            }
            else
            {
                if(m_TitleLabel != null)
                    m_TitleLabel.text = "Multiple Labels Selected";

                if (m_PresentConfigsListview != null)
                {
                    m_PresentConfigsListview.style.display = DisplayStyle.None;
                }

                if (m_CurrentlyPresentTitle != null)
                {
                    m_CurrentlyPresentTitle.style.display = DisplayStyle.None;
                }

                if (m_OtherConfigsTitle != null)
                {
                    m_OtherConfigsTitle.text = "All Label Configurations in Project:";
                }

                if (m_NonPresentConfigsListview != null)
                {
                    m_NonPresentConfigsListview.style.height = 250;
                }

                window.titleContent = new GUIContent("Manage Labels");
                window.minSize = new Vector2(350, 300);
                window.maxSize = new Vector2(350, 600);
            }

            window.Init();
        }


        void Init()
        {
            Show();

            m_ConfigsContainingLabel.Clear();

            m_LabelConfigTypes = FindAllSubTypes(typeof(LabelConfig<>));

            RefreshConfigAssets();
            CheckInclusionInConfigs(m_AllLabelConfigGuids, m_LabelValues.Count == 1? m_LabelValues.First() : null);
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
            if (m_LabelValues.Count == 1)
            {
                //we are dealing with only one label

                m_PresentConfigsListview.itemsSource = m_ConfigsContainingLabel;
                VisualElement MakeItem1() => new ConfigElementLabelPresent(this, m_LabelValues.First());

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
            }


            //Configs not containing label
            m_NonPresentConfigsListview.itemsSource = m_ConfigsNotContainingLabel;
            VisualElement MakeItem2() => new ConfigElementLabelNotPresent(this, m_LabelValues);

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
            CheckInclusionInConfigs(m_AllLabelConfigGuids, m_LabelValues.Count == 1? m_LabelValues.First() : null);
            m_PresentConfigsListview.Refresh();
            m_NonPresentConfigsListview.Refresh();
        }
        void OnEnable()
        {
            m_UxmlPath = m_UxmlDir + "AddToConfigWindow.uxml";

            m_Root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_UxmlPath).CloneTree(m_Root);

            m_TitleLabel = m_Root.Q<Label>("title");
            m_CurrentlyPresentTitle = m_Root.Q<Label>("currently-present-label");
            m_OtherConfigsTitle = m_Root.Q<Label>("other-configs-label");
            m_PresentConfigsListview = m_Root.Q<ListView>("current-configs-listview");
            m_NonPresentConfigsListview = m_Root.Q<ListView>("other-configs-listview");
            m_Status = m_Root.Q<Label>("status");
            m_Status.style.display = DisplayStyle.None;
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
        private string m_UxmlDir = "Packages/com.unity.perception/Editor/GroundTruth/Uxml/";
        private VisualElement m_Root;
        private ObjectField m_ConfigObjectField;

        public Label m_Label;
        public ScriptableObject m_LabelConfig;

        public ConfigElementLabelPresent(AddToConfigWindow window, string targetLabel)
        {
            var uxmlPath = m_UxmlDir + "ConfigElementLabelPresent.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("config-name");
            var removeButton = this.Q<Button>("remove-from-config-button");
            removeButton.text = "Remove Label";


            removeButton.clicked += () =>
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
        private VisualElement m_Root;
        private ObjectField m_ConfigObjectField;
        public Label m_Label;
        public ScriptableObject m_LabelConfig;

        public ConfigElementLabelNotPresent(AddToConfigWindow window, HashSet<string> targetLabels)
        {
            var uxmlPath = m_UxmlDir + "ConfigElementLabelPresent.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath).CloneTree(this);
            m_Label = this.Q<Label>("config-name");
            var addButton = this.Q<Button>("remove-from-config-button");
            addButton.text = targetLabels.Count ==1 ? "Add Label" : "Add All Labels";

            addButton.clicked += () =>
            {
                var methodInfo = m_LabelConfig.GetType().GetMethod(IdLabelConfig.AddLabelName);
                if (methodInfo != null)
                {
                    object[] parametersArray = new object[1];
                    foreach (var targetLabel in targetLabels)
                    {
                        parametersArray[0] = targetLabel;
                        methodInfo.Invoke(m_LabelConfig, parametersArray);
                    }
                    EditorUtility.SetDirty(m_LabelConfig);
                    window.RefreshLists();
                }

                if (targetLabels.Count > 1)
                {
                    window.SetStatus("All Labels Added to " + m_LabelConfig.name);
                }
            };
        }
    }
}
