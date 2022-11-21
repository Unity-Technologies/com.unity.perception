using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    /// <summary>
    /// Derive this class to force the Unity Editor to render the default inspector using UIElements for an Object that
    /// includes a Parameter field.
    /// to allow parameter UIs to render properly
    /// </summary>
    public abstract class ParameterUIElementsEditor : Editor
    {
        /// <summary>
        /// Creates proper VisualElement for Editor UI
        /// </summary>
        /// <returns>VisualElement</returns>
        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();
            UIElementsEditorUtilities.CreatePropertyFields(serializedObject, rootElement);
            return rootElement;
        }
    }
}
