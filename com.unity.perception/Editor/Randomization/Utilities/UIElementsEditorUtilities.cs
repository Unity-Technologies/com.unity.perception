using System;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.Randomization
{
    /// <summary>
    /// This class contains a set of helper functions for simplifying the creation of UI Elements editors
    /// </summary>
    static class UIElementsEditorUtilities
    {
        /// <summary>
        /// Creates a list of PropertyFields from the class fields of the given SerializedObject
        /// and adds them to the specified container element
        /// </summary>
        /// <param name="serializedObj">The SerializedObject to create property fields for</param>
        /// <param name="containerElement">The element to place the created PropertyFields in</param>
        public static void CreatePropertyFields(SerializedObject serializedObj, VisualElement containerElement)
        {
            var fieldType = serializedObj.targetObject.GetType();
            var iterator = serializedObj.GetIterator();
            iterator.NextVisible(true);
            if (iterator.NextVisible(false))
            {
                do
                {
                    var propertyField = CreatePropertyField(iterator, fieldType);
                    containerElement.Add(propertyField);
                } while (iterator.NextVisible(false));
            }
        }

        /// <summary>
        /// Creates a list of PropertyFields from the sub-fields of the given SerializedProperty
        /// and adds them to the specified container element
        /// </summary>
        /// <param name="property">The SerializedProperty to create sub property fields for</param>
        /// <param name="containerElement">The element to place the created PropertyFields in</param>
        public static void CreatePropertyFields(SerializedProperty property, VisualElement containerElement)
        {
            var obj = StaticData.GetManagedReferenceValue(property);
            if (obj == null)
                return;
            var fieldType = obj.GetType();
            var iterator = property.Copy();
            var nextSiblingProperty = property.Copy();
            nextSiblingProperty.NextVisible(false);
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, nextSiblingProperty))
                        break;
                    var propertyField = CreatePropertyField(iterator, fieldType);
                    containerElement.Add(propertyField);
                } while (iterator.NextVisible(false));
            }
        }

        /// <summary>
        /// Creates a PropertyField from a given SerializedProperty (with tooltips!)
        /// </summary>
        /// <param name="iterator">The SerializedProperty to create a PropertyField</param>
        /// <param name="parentPropertyType">The Type of the class encapsulating the provided SerializedProperty</param>
        /// <returns></returns>
        public static PropertyField CreatePropertyField(SerializedProperty iterator, Type parentPropertyType)
        {
            var propertyField = new PropertyField(iterator.Copy());
            propertyField.Bind(iterator.serializedObject);
            var originalField = parentPropertyType.GetField(iterator.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            var tooltipAttribute = originalField.GetCustomAttributes(true)
                .ToList().Find(att => att.GetType() == typeof(TooltipAttribute));
            if (tooltipAttribute != null)
                propertyField.tooltip = (tooltipAttribute as TooltipAttribute)?.tooltip;
            return propertyField;
        }
    }
}
