using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIElementsEditorUtilitiesTests
{
    //ScriptableObject so we can use ScriptableObject.CreateInstance()
    [Serializable]
    class TestType : ScriptableObject
    {
        public string publicField;
        [Tooltip("A tooltip")]
        public string publicFieldWithTooltip;
        [Tooltip("A tooltip")]
        [SerializeField]
        private string privateField;
        [Tooltip("A tooltip")]
        [SerializeField]
        protected string protectedField;
        [Tooltip("A tooltip")]
        [SerializeField]
        protected internal string protectedInternalField;
        [Tooltip("A tooltip")]
        [SerializeField]
        internal string internalField;
    }
    [Test]
    [TestCase(typeof(TestType), nameof(TestType.publicField))]
    [TestCase(typeof(TestType), nameof(TestType.publicFieldWithTooltip), "A tooltip")]
    [TestCase(typeof(TestType), "privateField", "A tooltip")]
    [TestCase(typeof(TestType), "protectedField", "A tooltip")]
    [TestCase(typeof(TestType), "protectedInternalField", "A tooltip")]
    [TestCase(typeof(TestType), "internalField", "A tooltip")]
    public void CreatePropertyField_ReturnsCorrectPropertyField_ForTypeAndField(Type type, string field,
        string tooltipExpected = "")
    {
        var testType = ScriptableObject.CreateInstance<TestType>();
        var serializedObject = new SerializedObject(testType);
        var serializedProperty = serializedObject.FindProperty(field);
        Assert.IsNotNull(serializedProperty);
        var propertyField = UnityEditor.Perception.Randomization.UIElementsEditorUtilities.CreatePropertyField(serializedProperty, type);
        Assert.IsNotNull(propertyField);
        Assert.AreEqual(tooltipExpected, propertyField.tooltip);
    }
}
