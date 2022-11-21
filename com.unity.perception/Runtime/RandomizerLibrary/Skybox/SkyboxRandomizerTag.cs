#if HDRP_PRESENT
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Supports the ability to randomize the HDRI Skybox (cubemap) of a volume from a set of skyboxes. The rotation of
    /// all skyboxes will be randomized individually based on the global setting of the <see cref="SkyboxRandomizer" />
    /// </summary>
    [AddComponentMenu("Perception/RandomizerTags/Skybox Randomizer Tag")]
    [RequireComponent(typeof(Volume))]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class SkyboxRandomizerTag : RandomizerTag
    {
        HDRISky m_Sky;
        /// <summary>
        /// The skybox attached to the volume. Currently, only HDRI skyboxes are supported.
        /// </summary>
        public HDRISky sky
        {
            get
            {
                if (m_Sky == null)
                    GetComponent<Volume>().profile.TryGet(out m_Sky);
                return m_Sky;
            }
            private set => m_Sky = value;
        }
    }
}
#endif
