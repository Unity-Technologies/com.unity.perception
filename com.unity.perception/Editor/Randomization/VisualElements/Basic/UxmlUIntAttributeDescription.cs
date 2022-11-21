using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.UIElements
{
    /// <summary>
    ///     <para>Describes a XML int attribute.</para>
    /// </summary>
    public class UxmlUIntAttributeDescription : TypedUxmlAttributeDescription<uint>
    {
        /// <summary>
        ///     <para>Constructor.</para>
        /// </summary>
        public UxmlUIntAttributeDescription()
        {
            type = "int";
            typeNamespace = "http://www.w3.org/2001/XMLSchema";
            defaultValue = 0;
        }

        /// <summary>
        ///     <para>The default value for the attribute, as a string.</para>
        /// </summary>
        public override string defaultValueAsString => defaultValue.ToString(CultureInfo.InvariantCulture.NumberFormat);

        /// <summary>
        ///     <para>
        ///         Retrieves the value of this attribute from the attribute bag. Returns it if it is found, otherwise return
        ///         defaultValue.
        ///     </para>
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <returns>
        ///     <para>The value of the attribute.</para>
        /// </returns>
        public override uint GetValueFromBag(IUxmlAttributes bag, CreationContext cc)
        {
            return GetValueFromBag(bag, cc, (s, i) => ConvertValueToUInt(s, i), defaultValue);
        }

        /// <summary>
        ///     <para>
        ///         Tries to retrieve the value of this attribute from the attribute bag. Returns it if it is found, otherwise return
        ///         defaultValue.
        ///     </para>
        /// </summary>
        /// <param name="bag">The bag of attributes.</param>
        /// <param name="cc">The context in which the values are retrieved.</param>
        /// <param name="value">Output value of the attribute</param>
        /// <returns>True if value was successfully received</returns>
        public bool TryGetValueFromBag(IUxmlAttributes bag, CreationContext cc, ref uint value)
        {
            return TryGetValueFromBag(bag, cc, (s, i) => ConvertValueToUInt(s, i), defaultValue, ref value);
        }

        static uint ConvertValueToUInt(string v, uint defaultValue)
        {
            return v == null || !uint.TryParse(v, out uint result) ? defaultValue : result;
        }
    }
}
