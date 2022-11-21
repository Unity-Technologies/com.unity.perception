using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    [MovedFrom("UnityEditor.Perception.Internal")]
    static class UiExtensions
    {
        /// <summary>
        /// Set the display property on a visual element based on the isShown boolean parameter.
        /// </summary>
        /// <param name="element">A visual element</param>
        /// <param name="isShown">Whether the element should be visible or not.</param>
        /// <returns>When isShown is true, set DisplayStyle to Flex and when false, set DisplayStyle to None.</returns>
        internal static void SetVisible(this VisualElement element, bool isShown)
        {
            if (element == null)
                return;

            element.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// For all children of root, assigns the tooltip that relates the child's binding-path to an existing
        /// SerializedProperty of the SerializedObject target. For example, a child VisualElement with the binding-path
        /// "state," will be assigned the Tooltip of the "state" property of target.
        /// </summary>
        /// <param name="root">The root visual element containing all children.</param>
        /// <param name="target">The serializedObject whose tooltips we will copy.</param>
        internal static void RecursivelyLoadTooltipsFromBoundProperties(VisualElement root, SerializedObject target)
        {
            // Generate a list of all VisualElements which can have bindings
            var elementQueue = new List<VisualElement>();
            void AddBindableElementsToQueue(VisualElement element)
            {
                if (element is IBindable bindableElement && !string.IsNullOrEmpty(bindableElement.bindingPath))
                    elementQueue.Add(element);
                element.Children().ToList().ForEach(AddBindableElementsToQueue);
            }

            AddBindableElementsToQueue(root);

            // Assign tooltips to VisualElements which also have a valid SerializedProperty (with an existing tooltip)
            foreach (var element in elementQueue)
            {
                if (element is IBindable bindableElement)
                {
                    var correspondingProperty = target.FindProperty(bindableElement.bindingPath);
                    if (correspondingProperty == null)
                    {
                        Debug.LogError("Could not load tooltips as binding-path does not have a corresponding field in the serialized object. Please make sure there are no typos in the corresponding UXML file.");
                        continue;
                    }

                    var soActualType = target.targetObject.GetType();
                    var soPropertyInfo = soActualType.GetField(bindableElement.bindingPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (soPropertyInfo == null)
                        continue;

                    var fieldTooltip = soPropertyInfo?.GetCustomAttributes(true)
                        .ToList().Find(att => att.GetType() == typeof(TooltipAttribute));
                    if (fieldTooltip == null)
                        continue;

                    element.tooltip = (fieldTooltip as TooltipAttribute)?.tooltip ?? "";
                }
            }
        }
    }
}
