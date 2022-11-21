using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Sets whether the transform operation occurs with respect to global or local space.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.Internal")]
    public enum TransformMethod
    {
        /// <summary>
        /// The GameObject is translated, rotated, or scaled exactly to the given values.
        /// </summary>
        /// <example>
        /// If the position on the X axis is being uniformly randomized between the values -5 and 5 where the GameObject
        /// is at position (200, 50, 100) in World-space. Then the range for its position during randomization would be
        /// between (-5, 50, 100) and (5, 50, 100).
        /// </example>
        Absolute,
        /// <summary>
        /// The translation or rotation from the given values is an offset from the original position or rotation. In
        /// the case of scaling, the given values act as a multiplier from the original local scale.
        /// </summary>
        /// <example>
        /// If the position on the X axis is being uniformly randomized between the values -5 and 5 where the GameObject
        /// is at position (200, 50, 100) in World-space. Then the range for its position during randomization would be
        /// between (195, 50, 100) and (205, 50, 100).
        /// </example>
        Relative
    }

    /// <summary>
    /// Supports the ability to randomize the position/translation, rotation, and scale of the target object.
    /// </summary>
    [AddComponentMenu("Perception/RandomizerTags/Transform Randomizer Tag")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class TransformRandomizerTag : RandomizerTag
    {
        #region Position
        /// <summary>
        /// When set to true, provides the ability to randomize an objects position on each axis.
        /// </summary>
        [Tooltip("When set to true, provides the ability to randomize an objects position on each axis.")]
        public bool shouldRandomizePosition = false;
        /// <remarks>
        /// Do not directly modify.
        /// </remarks>
        Vector3? m_OriginalPosition = null;
        /// <summary>
        /// Position of the GameObject at the start of the scenario.
        /// </summary>
        /// <remarks>
        /// This value is cached at the start of the scenario and is used when <see cref="positionMode" /> is set to
        /// "Relative" in order to generate randomized positions that are offset from it.
        /// </remarks>
        Vector3 originalPosition
        {
            get
            {
                if (m_OriginalPosition == null)
                    m_OriginalPosition = (transform.position);

                return m_OriginalPosition.Value;
            }
        }

        /// <summary>
        /// When <see cref="positionMode" /> is "Relative," then the values from <see cref="position" /> are used as
        /// offsets from the <see cref="originalPosition" />. When "Absolute," values are used as global coordinates for
        /// the GameObject.
        /// </summary>
        [Tooltip("When set to \"Relative,\" then values from randomization are applied as offsets to the original position of the GameObject. When set to \"Absolute,\" the values from randomization are set as the objects position.")]
        public TransformMethod positionMode = TransformMethod.Relative;
        /// <summary>
        /// The range of randomization for the target objects position.
        /// </summary>
        [Tooltip("The range of randomization for the target objects position.")]
        public Vector3Parameter position = new Vector3Parameter()
        {
            x = new ConstantSampler(0),
            y = new ConstantSampler(0),
            z = new ConstantSampler(0)
        };
        #endregion

        #region Rotation
        /// <summary>
        /// When set to true, provides the ability to randomize an objects rotation on each axis.
        /// </summary>
        [Tooltip("When set to true, provides the ability to randomize an objects rotation on each axis.")]
        public bool shouldRandomizeRotation = false;
        /// <remarks>
        /// Do not directly modify.
        /// </remarks>
        Vector3? m_OriginalRotation = null;
        /// <summary>
        /// Rotation of the GameObject at the start of the scenario.
        /// </summary>
        /// <remarks>
        /// This value is cached at the start of the scenario and is used when <see cref="rotationMode" /> is set to
        /// "Relative" in order to generate randomized rotations that are offset from it.
        /// </remarks>
        Vector3 originalRotation
        {
            get
            {
                if (m_OriginalRotation == null)
                    m_OriginalRotation = transform.rotation.eulerAngles;

                return m_OriginalRotation.Value;
            }
        }

        /// <summary>
        /// When <see cref="rotationMode" /> is "Relative," then the values from <see cref="rotation" /> are used as
        /// offsets from the <see cref="originalRotation" />. When "Absolute," values are used as Euler angles.
        /// </summary>
        [Tooltip("When set to \"Relative,\" then values from randomization are applied as offsets to the original rotation of the GameObject. When set to \"Absolute,\" the values from randomization are set as the objects rotation.")]
        public TransformMethod rotationMode = TransformMethod.Relative;
        /// <summary>
        /// The range of randomization for the target objects rotation.
        /// </summary>
        [Tooltip("The range of randomization for the target objects rotation.")]
        public Vector3Parameter rotation = new Vector3Parameter()
        {
            x = new ConstantSampler(0),
            y = new ConstantSampler(0),
            z = new ConstantSampler(0)
        };
        #endregion

        #region Vector 3 Scale
        /// <summary>
        /// When set to true, provides the ability to randomize an objects scale on each axis.
        /// </summary>
        [Tooltip("When set to true, provides the ability to randomize an objects scale on each axis.")]
        public bool shouldRandomizeScale = false;
        /// <remarks>
        /// Do not directly modify.
        /// </remarks>
        Vector3? m_OriginalScale = null;
        /// <summary>
        /// Scale of the GameObject at the start of the scenario.
        /// </summary>
        /// <remarks>
        /// This value is cached at the start of the scenario and is used when <see cref="scaleMode" /> is set to
        /// "Relative" in order to generate scale values that are multiples of it.
        /// </remarks>
        Vector3 originalScale
        {
            get
            {
                if (m_OriginalScale == null)
                    m_OriginalScale = transform.localScale;
                return m_OriginalScale.Value;
            }
        }

        /// <summary>
        /// When <see cref="scaleMode" /> is "Relative," then the values from <see cref="scale" /> pr
        /// <see cref="uniformScale"/> are used as multipliers to the <see cref="originalScale" />. When "Absolute,"
        /// then values are used as the scale of the GameObject.
        /// </summary>
        [Tooltip("When set to \"Relative,\" then values from randomization are applied as multipliers to the original scale of the GameObject. When set to \"Absolute,\" the values from randomization are set as the objects scale.")]
        public TransformMethod scaleMode = TransformMethod.Relative;
        /// <summary>
        /// The range of randomization for the target objects scale, customizable on a per-axis level.
        /// </summary>
        [Tooltip("The range of randomization for the target objects scale, customizable on a per-axis level.")]
        public Vector3Parameter scale = new Vector3Parameter()
        {
            x = new ConstantSampler(1),
            y = new ConstantSampler(1),
            z = new ConstantSampler(1)
        };
        #endregion

        #region Uniform Scale
        /// <summary>
        /// When true, each axis of scale will have the same randomized value each iteration. Otherwise when false,
        /// each axis is randomly scaled independently.
        /// </summary>
        [Tooltip("When true, each axis of scale will have the same randomized value each iteration. Otherwise when false, each axis is randomly scaled independently. ")]
        public bool useUniformScale = false;
        /// <summary>
        /// The range of randomization for the target objects scale.
        /// </summary>
        [Tooltip("The range of randomization for the target objects scale.")]
        public FloatParameter uniformScale = new FloatParameter()
        {
            value = new ConstantSampler(1)
        };
        #endregion

        /// <summary>
        /// Randomizes the position, rotation, and scale of the GameObject based on the tag configuration.
        /// </summary>
        public void Randomize()
        {
            // Randomize position
            if (shouldRandomizePosition)
            {
                transform.position = (positionMode == TransformMethod.Relative ? originalPosition : Vector3.zero) + position.Sample();
            }

            // Randomize rotation
            if (shouldRandomizeRotation)
            {
                transform.rotation = Quaternion.Euler((rotationMode == TransformMethod.Relative ? originalRotation : Vector3.zero) + rotation.Sample());
            }

            // Randomize scale
            if (shouldRandomizeScale)
            {
                var newScale = (scaleMode == TransformMethod.Relative ? originalScale : Vector3.one);
                if (useUniformScale)
                {
                    var scaleFactor = uniformScale.Sample();
                    newScale.Scale(new Vector3(scaleFactor, scaleFactor, scaleFactor));
                }
                else
                {
                    newScale.Scale(scale.Sample());
                }

                transform.localScale = newScale;
            }
        }
    }
}
