using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.Settings;

namespace UnityEditor.Perception.GroundTruth
{
    /// <summary>
    /// Editor UI to represent possible consumers endpoints
    /// </summary>
    [CustomPropertyDrawer(typeof(ConsumerEndpointDrawerAttribute))]
    public class ConsumerEndpointDrawer : PropertyDrawer
    {
        const int k_PaddingAmount = 5;

        /// <summary>
        /// Build editor UI to represent possible consumers endpoints. Called by Unity editor
        /// </summary>
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(position, label, property);

            var settings = property.serializedObject.targetObject as PerceptionSettings;
            var endpoint = settings != null ? settings.consumerEndpoint : null;
            var serializedObject = property.serializedObject;

            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (endpoint == null)
            {
                var s = new GUIStyle(EditorStyles.boldLabel);
                position.height = height;
                EditorGUI.LabelField(position, "No active endpoint", s);
            }
            else
            {
                var s = new GUIStyle(EditorStyles.boldLabel);
                position.height = height;
                EditorGUI.LabelField(position, ObjectNames.NicifyVariableName(endpoint.GetType().Name), s);
                position.y += height;

                EditorGUI.indentLevel += 1;

                foreach (SerializedProperty prop in property)
                {
                    EditorGUI.PropertyField(position, prop);
                    position.y += height + k_PaddingAmount;
                }

                if (endpoint is IFileSystemEndpoint fsEndpoint)
                {
                    // Create a field with a name, text box - un-editable, and a choose path button
                    var p = EditorGUI.PrefixLabel(position, new GUIContent("Base Path"));

                    // if the path is set to the default path, then display what the default path will be
                    // on the user's machine
                    var path = fsEndpoint.basePath;

                    // Selectable label boxes are 4 pixels narrower than other boxes, this is just to align it
                    // with other entries
                    p.x -= 15;
                    p.width += 15;
                    EditorGUI.SelectableLabel(p, path, EditorStyles.textField);

                    position.y += height + k_PaddingAmount;

                    var buttonRect = EditorGUI.IndentedRect(position);
                    var width = buttonRect.width / 3f;
                    buttonRect.width = width;

                    if (GUI.Button(buttonRect, "Change Folder"))
                    {
                        path = EditorUtility.OpenFolderPanel("Choose Output Folder", "", "");
                        if (path.Length != 0)
                        {
                            fsEndpoint.basePath = path;
                            serializedObject.Update();
                        }
                    }

                    buttonRect.x += width;

                    if (GUI.Button(buttonRect, "Show Folder"))
                    {
                        EditorUtility.RevealInFinder(path);
                    }

                    buttonRect.x += width;

                    if (GUI.Button(buttonRect, "Reset To Default"))
                    {
                        fsEndpoint.basePath = fsEndpoint.defaultPath;
                        serializedObject.Update();
                    }

                    position.y += height + k_PaddingAmount;
                }

                EditorGUI.indentLevel -= 1;

                if (!endpoint.IsValid(out var errorMsg))
                {
                    position.y += k_PaddingAmount;
                    position.height = height * 2;
                    EditorGUI.HelpBox(position, errorMsg, MessageType.Error);
                    position.y += height * 2 + k_PaddingAmount;
                }

                EditorGUI.EndProperty();

                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Returns a property height for valid endpoints
        /// </summary>
        /// <param name="property">Property to be used for calculation</param>
        /// <param name="label">Label to draw</param>
        /// <returns></returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var settings = property.serializedObject.targetObject as PerceptionSettings;
            var endpoint = settings != null ? settings.consumerEndpoint : null;

            var p = property.Copy();
            var padding = 0;

            var count = 1;
            if (endpoint != null)
            {
                foreach (var prop in p) count++;
            }

            // if this is an IFileSystemEndpoint we need to add in the height for the button row
            if (endpoint is IFileSystemEndpoint)
            {
                count += 2;
                padding += k_PaddingAmount * 4;
            }

            // if there is an error, we need to add in an error message box
            if (endpoint != null && !endpoint.IsValid(out var _))
            {
                count += 2;
                padding += 2;
            }

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * count + padding;
        }
    }
}
