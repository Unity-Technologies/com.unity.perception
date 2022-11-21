using UnityEngine;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// The base asset role class. Derive from <see cref="AssetRole{T}"/> instead to create a new asset role.
    /// </summary>
    interface IAssetRoleBase
    {
        /// <summary>
        /// The string label uniquely associated with this asset role
        /// </summary>
        string label { get; }

        /// <summary>
        /// A description for this asset role
        /// </summary>
        string description { get; }
    }
}
