using System;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.UIElements
{
    /// <summary>
    ///     <para>Makes a text field for entering an unsigned integer.</para>
    /// </summary>
    public class UIntField : TextValueField<uint>
    {
        /// <summary>
        ///     <para>USS class name of elements of this type.</para>
        /// </summary>
        public new static readonly string ussClassName = "unity-uint-field";
        /// <summary>
        ///     <para>USS class name of labels in elements of this type.</para>
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        ///     <para>USS class name of input elements in elements of this type.</para>
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        ///     <para>Constructor.</para>
        /// </summary>
        public UIntField()
            : this(null)
        {
            AddToClassList("unity-base-field__aligned");
        }

        /// <summary>
        ///     <para>Constructor.</para>
        /// </summary>
        /// <param name="maxLength">Maximum number of characters the field can take.</param>
        public UIntField(int maxLength) : this(null, maxLength) {}

        public UIntField(string label, int maxLength = -1) : base(label, maxLength, new UIntInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            labelElement.AddToClassList("unity-property-field__label");
            AddLabelDragger<uint>();
        }

        UIntInput uIntInput => (UIntInput)textInputBase;

        /// <summary>
        ///     <para>Converts the given uint to a string.</para>
        /// </summary>
        /// <param name="v">The uint to be converted to string.</param>
        /// <returns>
        ///     <para>The uint as string.</para>
        /// </returns>
        protected override string ValueToString(uint v)
        {
            return v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        ///     <para>Converts a string to an uint.</para>
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>
        ///     <para>The uint parsed from the string.</para>
        /// </returns>
        protected override uint StringToValue(string str)
        {
            long.TryParse(str, out var result);
            return ClampInput(result);
        }

        /// <summary>
        ///     <para>Modify the value using a 3D delta and a speed, typically coming from an input device.</para>
        /// </summary>
        /// <param name="delta">A vector used to compute the value change.</param>
        /// <param name="speed">A multiplier for the value change.</param>
        /// <param name="startValue">The start value.</param>
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, uint startValue)
        {
            uIntInput.ApplyInputDeviceDelta(delta, speed, startValue);
        }

        /// <summary>
        ///     <para>Instantiates an UIntField using the data read from a UXML file.</para>
        /// </summary>
        public new class UxmlFactory : UxmlFactory<UIntField, UxmlTraits> {}

        /// <summary>
        ///     <para>Defines UxmlTraits for the UIntField.</para>
        /// </summary>
        public new class UxmlTraits : TextValueFieldTraits<uint, UxmlUIntAttributeDescription> {}

        public static uint ClampInput(long input)
        {
            input = input > uint.MaxValue ? uint.MaxValue : input;
            input = input < uint.MinValue ? uint.MinValue : input;
            return (uint)input;
        }

        class UIntInput : TextValueInput
        {
            internal UIntInput()
            {
                formatString = "#######0";
            }

            UIntField parentUIntField => (UIntField)parent;

            protected override string allowedCharacters => "0123456789";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, uint startValue)
            {
                var num = StringToValue(text) + (long)Math.Round(delta.x);
                var value = ClampInput(num);
                if (parentUIntField.isDelayed)
                    text = ValueToString(value);
                else
                    parentUIntField.value = value;
            }

            protected override string ValueToString(uint v)
            {
                return v.ToString(formatString);
            }

            protected override uint StringToValue(string str)
            {
                long.TryParse(str, out var result);
                return ClampInput(result);
            }
        }
    }
}
