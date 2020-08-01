using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.Randomization.Editor
{
    [CustomEditor(typeof(ParameterConfiguration))]
    public class ParameterConfigurationEditor : UnityEditor.Editor
    {
        ParameterConfiguration m_Config;
        VisualElement m_Root;
        VisualElement m_ParameterContainer;

        const string k_FoldoutOpenClass = "foldout-open";
        const string k_FoldoutClosedClass = "foldout-closed";

        string m_FilterString = "";
        public string FilterString
        {
            get => m_FilterString;
            set
            {
                m_FilterString = value;
                var lowerFilter = m_FilterString.ToLower();
                foreach (var child in m_ParameterContainer.Children())
                {
                    var paramIndex = m_ParameterContainer.IndexOf(child);
                    var param = m_Config.parameters[paramIndex];
                    ((ParameterElement)child).Filtered = param.parameterName.ToLower().Contains(lowerFilter);
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Config = (ParameterConfiguration)target;
            m_Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/ParameterConfiguration.uxml").CloneTree();

            m_ParameterContainer = m_Root.Query<VisualElement>("parameters-container").First();

            foreach (var parameter in m_Config.parameters)
                m_ParameterContainer.Add(new ParameterElement(parameter, this));

            var parameterTypeMenu = m_Root.Query<ToolbarMenu>("parameter-type-menu").First();
            foreach (var parameterType in StaticData.parameterTypes)
            {
                parameterTypeMenu.menu.AppendAction(
                    ParameterMetaData.GetMetaData(parameterType).typeDisplayName,
                    a => { AddParameter(parameterType); },
                    a => DropdownMenuAction.Status.Normal);
            }

            var filter = m_Root.Query<TextField>("filter-parameters").First();
            filter.RegisterValueChangedCallback((e) => { FilterString = e.newValue; });

            var collapseAllButton = m_Root.Query<Button>("collapse-all").First();
            collapseAllButton.clicked += () => CollapseParameters(true);

            var expandAllButton = m_Root.Query<Button>("expand-all").First();
            expandAllButton.clicked += () => CollapseParameters(false);

            return m_Root;
        }

        void CollapseParameters(bool collapsed)
        {
            foreach (var child in m_ParameterContainer.Children())
                ((ParameterElement)child).Collapsed = collapsed;
        }

        void AddParameter(Type parameterType)
        {
            var parameter = (Parameter)m_Config.gameObject.AddComponent(parameterType);
            foreach (var sampler in parameter.Samplers)
            {
                if (sampler is RandomSampler randomSampler)
                    randomSampler.seed = (uint)Random.Range(1, int.MaxValue);
            }
            parameter.hideFlags = HideFlags.HideInInspector;
            m_Config.parameters.Add(parameter);
            m_ParameterContainer.Add(new ParameterElement(parameter, this));
        }

        public void RemoveParameter(VisualElement template)
        {
            var paramIndex = m_ParameterContainer.IndexOf(template);
            m_ParameterContainer.RemoveAt(paramIndex);

            var param = m_Config.parameters[paramIndex];
            m_Config.parameters.RemoveAt(paramIndex);

            DestroyImmediate(param);
        }

        public void ReorderParameter(int currentIndex, int nextIndex)
        {
            if (currentIndex == nextIndex)
                return;

            if (nextIndex > currentIndex)
                nextIndex--;

            var parameterElement = m_ParameterContainer[currentIndex];
            var parameter = m_Config.parameters[currentIndex];

            parameterElement.RemoveFromHierarchy();
            m_Config.parameters.RemoveAt(currentIndex);

            m_ParameterContainer.Insert(nextIndex, parameterElement);
            m_Config.parameters.Insert(nextIndex, parameter);
        }
    }
}
