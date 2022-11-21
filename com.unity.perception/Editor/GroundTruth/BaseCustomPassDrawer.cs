#if HDRP_PRESENT

using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;

namespace UnityEditor.Perception.GroundTruth
{
    public class BaseCustomPassDrawer : CustomPassDrawer
    {
        List<SerializedProperty> m_CustomPassUserProperties = new List<SerializedProperty>();
        static float s_DefaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        protected void AddProperty(SerializedProperty property)
        {
            m_CustomPassUserProperties.Add(property);
        }

        protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
        {
            foreach (var prop in m_CustomPassUserProperties)
            {
                EditorGUI.PropertyField(rect, prop);
                rect.y += s_DefaultLineSpace;
            }
        }

        protected override float GetPassHeight(SerializedProperty customPass)
        {
            float height = 0f;
            foreach (var prop in m_CustomPassUserProperties)
            {
                height += EditorGUI.GetPropertyHeight(prop);
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
#endif
