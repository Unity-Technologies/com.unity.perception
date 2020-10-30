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

        private List<string> m_ConfigsContainingLabel = new List<string>();
        private List<string> m_ConfigsNotContainingLabel = new List<string>();

        private ListView m_PresentConfigsListview;
        private ListView m_NonPresentConfigsListview;
        public static void ShowWindow(string labelValue)
        {
            m_LabelValue = labelValue;
            var window = GetWindow<AddToConfigWindow>();
            window.Init();
            window.Show();
        }

        void Init()
        {
            m_ConfigsContainingLabel.Clear();

            if(m_TitleLabel != null)
                m_TitleLabel.text = "Add " + m_LabelValue + "to Label Configurations";

            var types = FindAllRelevant();

            AssetDatabase.Refresh();

            List<string> labelConfigGuids = new List<string>();
            foreach (var type in types)
            {
                labelConfigGuids.AddRange(AssetDatabase.FindAssets("t:"+type.Name));
            }

            CheckInclusionInConfigs(labelConfigGuids, types, m_LabelValue, this);

            Func<VisualElement> makeItem = () => new Label();
            Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = m_ConfigsContainingLabel[i];

            m_PresentConfigsListview.itemHeight = 30;
            m_PresentConfigsListview.itemsSource = m_ConfigsContainingLabel;

            m_PresentConfigsListview.bindItem = bindItem;
            m_PresentConfigsListview.makeItem = makeItem;


            Func<VisualElement> makeItem1 = () => new Label();
            Action<VisualElement, int> bindItem1 = (e, i) => (e as Label).text = m_ConfigsNotContainingLabel[i];

            m_NonPresentConfigsListview.itemHeight = 30;
            m_NonPresentConfigsListview.itemsSource = m_ConfigsNotContainingLabel;

            m_NonPresentConfigsListview.bindItem = bindItem1;
            m_NonPresentConfigsListview.makeItem = makeItem1;
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
            foreach (var configGuid in configGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(configGuid));
                var methodInfo = asset.GetType().GetMethod("DoesLabelMatchAnyEntries");

                if (methodInfo == null)
                    continue;

                object[] parametersArray = new object[1];
                parametersArray[0] = label;

                var labelExistsInConfig = (bool) methodInfo.Invoke(asset, parametersArray);

                if (labelExistsInConfig)
                {
                    m_ConfigsContainingLabel.Add(asset.name);
                }
                else
                {
                    m_ConfigsNotContainingLabel.Add(asset.name);
                }
            }
        }

        public T Cast<T>(object o)
        {
            return (T) o;
        }

        List<Type> FindAllRelevant()
        {
            Type superType = typeof(LabelConfig<>);
            Assembly assembly = Assembly.GetAssembly(superType);
            Type[] types = assembly.GetTypes();
            List<Type> subclasses = types.Where(t => IsSubclassOfRawGeneric(superType, t)).ToList();

            return subclasses;

        }

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
