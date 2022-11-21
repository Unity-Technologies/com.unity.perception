using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using FloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;
using Vector4Parameter = UnityEngine.Perception.Randomization.Parameters.Vector4Parameter;

namespace UnityEngine.Perception.Utilities
{
    /// <summary>
    /// A struct containing the name, description, and property type of a shader property.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEditor.Perception.Internal")]
    public abstract class ShaderPropertyEntry
    {
        /// <summary>
        /// The name of the shader property (eg: "_BaseMap").
        /// </summary>
        public string name;
        /// <summary>
        /// The description of the shader property (eg: "Albedo").
        /// </summary>
        public string description;
        /// <summary>
        /// The index of the shader property with which functions such as GetPropertyName are called (eg: 5).
        /// </summary>
        public int index = -1;

        /// <summary>
        /// The data type of the shader property.
        /// Available options: Float, Range, Color, Texture, Vector.
        /// </summary>
        /// <returns>
        /// The property type of the supported shader property.
        /// </returns>
        public abstract ShaderPropertyType SupportedShaderPropertyType();

        /// <summary>
        /// A reference to the parameter that is used to sample a value for the shader property.
        /// </summary>
        public abstract Parameter uiParameter { get; }

        /// <summary>
        /// Collates information such as name, description, type, and flags of a shader property into one class.
        /// </summary>
        /// <param name="shader">Name of the shader</param>
        /// <param name="propertyIndex">Index of the shader property</param>
        /// <returns>A class containing collated information of the shader property</returns>
        /// <remarks>
        /// <see cref="ShaderPropertyEntry" /> is required as names, descriptions, etc. of a shader property are
        /// only available as disparate function calls rather than an encapsulated data types.
        /// </remarks>
        public static ShaderPropertyEntry FromShaderPropertyIndex(Shader shader, int propertyIndex)
        {
            var shaderName = shader.GetPropertyName(propertyIndex);
            var shaderDescription = shader.GetPropertyDescription(propertyIndex);
            var shaderType = shader.GetPropertyType(propertyIndex);
            var shaderFlags = shader.GetPropertyFlags(propertyIndex);

            if (shaderFlags == ShaderPropertyFlags.NonModifiableTextureData)
                return null;

            switch (shaderType)
            {
                case ShaderPropertyType.Float:
                    return new FloatShaderPropertyEntry()
                    {
                        name = shaderName, description = shaderDescription, index = propertyIndex
                    };
                case ShaderPropertyType.Range:
                    return new RangeShaderPropertyEntry()
                    {
                        name = shaderName, description = shaderDescription, index = propertyIndex,
                        range = shader.GetPropertyRangeLimits(propertyIndex)
                    };
                case ShaderPropertyType.Texture:
                    return new TextureShaderPropertyEntry()
                    {
                        name = shaderName, description = shaderDescription, index = propertyIndex,
                    };
                case ShaderPropertyType.Color:
                    return new ColorShaderPropertyEntry()
                    {
                        name = shaderName, description = shaderDescription, index = propertyIndex
                    };
                case ShaderPropertyType.Vector:
                    return new VectorPropertyEntry()
                    {
                        name = shaderName, description = shaderDescription, index = propertyIndex,
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Override comparing with other objects
        /// </summary>
        /// <param name="obj">Any object</param>
        /// <returns>True if RLibShaderProperty and names are equal</returns>
        public override bool Equals(object obj)
        {
            return (obj is ShaderPropertyEntry otherProp) && otherProp.name == name;
        }

        /// <summary>
        /// Custom hash calculation
        /// </summary>
        /// <returns>Custom hash code</returns>
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    /// <summary>
    /// Stores information about a shader property of type <see cref="ShaderPropertyType.Float"/>.
    /// </summary>
    class FloatShaderPropertyEntry : ShaderPropertyEntry
    {
        public FloatParameter parameter = new FloatParameter()
        {
            value = new UniformSampler(0f, 1f)
        };
        public override ShaderPropertyType SupportedShaderPropertyType() => ShaderPropertyType.Float;
        public override Parameter uiParameter => parameter;
    }

    /// <summary>
    /// Stores information about a shader property of type <see cref="ShaderPropertyType.Texture"/>.
    /// </summary>
    class TextureShaderPropertyEntry : ShaderPropertyEntry
    {
        public CategoricalParameter<Texture2D> parameter = new CategoricalParameter<Texture2D>();
        public override ShaderPropertyType SupportedShaderPropertyType() => ShaderPropertyType.Texture;
        public override Parameter uiParameter => parameter;
    }

    /// <summary>
    /// Stores information about a shader property of type <see cref="ShaderPropertyType.Range"/>.
    /// </summary>
    class RangeShaderPropertyEntry : ShaderPropertyEntry
    {
        public FloatParameter parameter = new FloatParameter()
        {
            value = new UniformSampler(0f, 1f)
        };
        public Vector2 range = new Vector2(0f, 1f);
        public override ShaderPropertyType SupportedShaderPropertyType() => ShaderPropertyType.Range;
        public override Parameter uiParameter => parameter;
    }

    /// <summary>
    /// Stores information about a shader property of type <see cref="ShaderPropertyType.Color"/>.
    /// </summary>
    class ColorShaderPropertyEntry : ShaderPropertyEntry
    {
        public CategoricalParameter<Color> parameter = new CategoricalParameter<Color>();
        public override ShaderPropertyType SupportedShaderPropertyType() => ShaderPropertyType.Color;
        public override Parameter uiParameter => parameter;
    }

    /// <summary>
    /// Stores information about a shader property of type <see cref="ShaderPropertyType.Vector" />.
    /// </summary>
    class VectorPropertyEntry : ShaderPropertyEntry
    {
        public Vector4Parameter parameter = new Vector4Parameter();
        public override ShaderPropertyType SupportedShaderPropertyType() => ShaderPropertyType.Vector;
        public override Parameter uiParameter => parameter;
    }
}
