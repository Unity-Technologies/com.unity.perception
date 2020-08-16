using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomEditor(typeof(ParameterConfiguration))]
    class ParameterConfigurationEditor : UnityEditor.Editor
    {
        VisualElement m_Root;
        VisualElement m_ParameterContainer;

        public ParameterConfiguration config;

        string m_FilterString = string.Empty;
        public string FilterString
        {
            get => m_FilterString;
            private set
            {
                m_FilterString = value;
                var lowerFilter = m_FilterString.ToLower();
                foreach (var child in m_ParameterContainer.Children())
                {
                    var paramIndex = m_ParameterContainer.IndexOf(child);
                    var param = config.parameters[paramIndex];
                    ((ParameterElement)child).Filtered = param.name.ToLower().Contains(lowerFilter);
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            config = (ParameterConfiguration)target;
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterConfiguration.uxml").CloneTree();

            m_ParameterContainer = m_Root.Q<VisualElement>("parameters-container");

            var filter = m_Root.Q<TextField>("filter-parameters");
            filter.RegisterValueChangedCallback((e) => { FilterString = e.newValue; });

            var collapseAllButton = m_Root.Q<Button>("collapse-all");
            collapseAllButton.clicked += () => CollapseParameters(true);

            var expandAllButton = m_Root.Q<Button>("expand-all");
            expandAllButton.clicked += () => CollapseParameters(false);

            var parameterTypeMenu = m_Root.Q<ToolbarMenu>("parameter-type-menu");
            foreach (var parameterType in StaticData.parameterTypes)
            {
                parameterTypeMenu.menu.AppendAction(
                    Parameter.GetDisplayName(parameterType),
                    a => { AddParameter(parameterType); },
                    a => DropdownMenuAction.Status.Normal);
            }

            RefreshParameterElements();

            return m_Root;
        }

        void RefreshParameterElements()
        {
            m_ParameterContainer.Clear();
            for (var i = 0; i < config.parameters.Count; i++)
                m_ParameterContainer.Add(CreateParameterElement(i));
        }

        ParameterElement CreateParameterElement(int index)
        {
            return new ParameterElement(index, this);
        }

        void AddParameter(Type parameterType)
        {
            var parameter = config.AddParameter(parameterType);
            parameter.RandomizeSamplers();

            serializedObject.Update();
            m_ParameterContainer.Add(CreateParameterElement(config.parameters.Count - 1));
        }

        public void RemoveParameter(VisualElement template)
        {
            var paramIndex = m_ParameterContainer.IndexOf(template);
            m_ParameterContainer.RemoveAt(paramIndex);
            config.parameters.RemoveAt(paramIndex);
            serializedObject.Update();
            RefreshParameterElements();
        }

        public void ReorderParameter(int currentIndex, int nextIndex)
        {
            if (currentIndex == nextIndex)
                return;

            if (nextIndex > currentIndex)
                nextIndex--;

            var parameterElement = m_ParameterContainer[currentIndex];
            var parameter = config.parameters[currentIndex];

            parameterElement.RemoveFromHierarchy();
            config.parameters.RemoveAt(currentIndex);

            m_ParameterContainer.Insert(nextIndex, parameterElement);
            config.parameters.Insert(nextIndex, parameter);

            serializedObject.Update();

            RefreshParameterElements();
        }

        void CollapseParameters(bool collapsed)
        {
            foreach (var child in m_ParameterContainer.Children())
                ((ParameterElement)child).Collapsed = collapsed;
        }
    }
}
