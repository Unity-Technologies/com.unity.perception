namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Object that represent the data will be passed during the cloud simulation
    /// </summary>
    public class SimulationMetadata : Metadata
    {
        const string k_UnityVersionLabel = "unityVersion";
        const string k_PerceptionVersionLabel = "perceptionVersion";
        const string k_RenderPipelineLabel = "renderPipeline";

        /// <summary>
        /// Creates a new simulation metadata
        /// </summary>
        public SimulationMetadata()
        {
            unityVersion = "not_set";
            perceptionVersion = "not_set";
#if HDRP_PRESENT
            renderPipeline = "HDRP";
#else
            renderPipeline = "built-in";
#endif
        }

        /// <summary>
        /// The version of the Unity editor executing the simulation.
        /// </summary>
        public string unityVersion
        {
            get => GetString(k_UnityVersionLabel);
            set => Add(k_UnityVersionLabel, value);
        }

        /// <summary>
        /// The version of the perception package used to generate the data.
        /// </summary>
        public string perceptionVersion
        {
            get => GetString(k_PerceptionVersionLabel);
            set => Add(k_PerceptionVersionLabel, value);
        }

        /// <summary>
        /// The render pipeline used to create the data. Currently either URP or HDRP.
        /// </summary>
        public string renderPipeline
        {
            get => GetString(k_RenderPipelineLabel);
            private set => Add(k_RenderPipelineLabel, value);
        }
    }
}
