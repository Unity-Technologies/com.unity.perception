using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
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
        string FilterString
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

            m_ParameterContainer = m_Root.Query<VisualElement>("parameter-container").First();

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

            var loadAdrConfigButton = m_Root.Query<Button>("load-adr-config").First();
            loadAdrConfigButton.clicked += () => m_Config.Deserialize();

            var serializeAdrConfigButton = m_Root.Query<Button>("write-adr-config").First();
            serializeAdrConfigButton.clicked += () => m_Config.Serialize();

            var filter = m_Root.Query<TextField>("filter-parameters").First();
            filter.RegisterValueChangedCallback((e) => { FilterString = e.newValue; });

            var collapseParam = m_Root.Query<VisualElement>("collapse-all").First();
            collapseParam.RegisterCallback<MouseUpEvent>(evt => CollapseAllParameters(collapseParam));

            return m_Root;
        }

        void CollapseAllParameters(VisualElement collapse)
        {
            var collapsing = collapse.ClassListContains(k_FoldoutOpenClass);
            if (collapsing)
            {
                collapse.AddToClassList(k_FoldoutClosedClass);
                collapse.RemoveFromClassList(k_FoldoutOpenClass);
            }
            else
            {
                collapse.AddToClassList(k_FoldoutOpenClass);
                collapse.RemoveFromClassList(k_FoldoutClosedClass);
            }
            foreach (var child in m_ParameterContainer.Children())
                ((ParameterElement)child).Collapsed = collapsing;
        }

        void AddParameter(Type parameterType)
        {
            var parameter = (Parameter)m_Config.gameObject.AddComponent(parameterType);
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

        public void MoveParameter(VisualElement template, int direction)
        {
            if (FilterString != "")
                return;
            var paramIndex = m_ParameterContainer.IndexOf(template);
            if (direction == -1 && paramIndex > 0)
            {
                SwapParameters(paramIndex - 1, paramIndex);
            }
            else if (direction == 1 && paramIndex < m_Config.parameters.Count - 1)
            {
                SwapParameters(paramIndex, paramIndex + 1);
            }
        }

        void SwapParameters(int first, int second)
        {
            var firstElement = m_ParameterContainer[first];
            var secondElement = m_ParameterContainer[second];
            m_ParameterContainer.RemoveAt(second);
            m_ParameterContainer.RemoveAt(first);
            m_ParameterContainer.Insert(first, secondElement);
            m_ParameterContainer.Insert(second, firstElement);

            var firstParameter = m_Config.parameters[first];
            var secondParameter = m_Config.parameters[second];
            m_Config.parameters[first] = secondParameter;
            m_Config.parameters[second] = firstParameter;
        }
    }
}
