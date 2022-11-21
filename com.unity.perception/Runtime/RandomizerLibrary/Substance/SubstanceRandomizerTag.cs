#if SUBSTANCE_PLUGIN_ENABLED
using System;
using System.Collections.Generic;
using Substance.Game;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    [MovedFrom("UnityEngine.Perception.Internal")]
    public enum SubstanceRandomizationMode
    {
        /// <summary>
        /// The properties of the substance graph are randomized which regenerates the associated material. This affects
        /// all objects that share the original generated material by the common substance graph.
        /// </summary>
        Graph,
        /// <summary>
        /// A copy of the substance graph is made before properties are randomized. Thus, as a new material is
        /// created, the randomization only targets this particular object and not others.
        /// </summary>
        Instance
    }

    /// <summary>
    /// At each iteration, automatically randomize a subset of the chosen parameters for the substance graph.
    /// </summary>
    /// <remarks>
    /// If <see cref="parametersToRandomize"/> is empty, the substance will not be randomized at any iteration.
    /// </remarks>
    [AddComponentMenu("Perception/RandomizerTags/Substance Randomizer Tag")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class SubstanceRandomizerTag : RandomizerTag
    {
        /// <summary>
        /// The substance whose properties will be randomized.
        /// </summary>
        [SerializeField]
        [Tooltip("The substance whose properties will be randomized.")]
        SubstanceGraph m_SubstanceGraph;
        /// <summary>
        /// Whether the substance graph randomization affects all objects with the shared substance/material or only the
        /// particular object whose substance graph is being randomized.
        /// </summary>
        [Tooltip("Whether the substance graph randomization affects all objects with the shared substance/material (Graph mode) or only the particular object whose substance graph is being randomized (Instance Mode).")]
        public SubstanceRandomizationMode mode = SubstanceRandomizationMode.Graph;
        /// <summary>
        /// The parameters to randomize. If the parameter is not found in the substance, it will be skipped. Otherwise,
        /// it will be randomized based on the constraints defined in the substance itself.
        /// </summary>
        [Tooltip("The list of substance graph parameters to randomizer within the constraints defined in the substance.")]
        public List<string> parametersToRandomize;

        /// <summary>
        /// When <see cref="mode" /> is "Instance," then <see cref="m_Initialized" /> is true if and only if a copy of
        /// <see cref="m_SubstanceGraph" /> has been made for randomization.
        /// </summary>
        bool m_Initialized;
        /// <summary>
        /// The copy of <see cref="m_SubstanceGraph" />. Only used when <see cref="mode" /> is "Instance."
        /// </summary>
        SubstanceGraph m_InternalGraph;
        /// <summary>
        /// The index of the material element whose attached material is associated with <see cref="m_SubstanceGraph" />.
        /// </summary>
        int m_SubstanceGraphMaterialIndex = -1;

        /// <summary>
        /// The selected substance graph associated with the tag at edit time.
        /// </summary>
        public SubstanceGraph graph
        {
            get => m_SubstanceGraph;
            set
            {
                m_SubstanceGraph = value;
                m_Initialized = false;
            }
        }

        /// <summary>
        /// The selected substance graph (or a duplicate of it) associated with the tag at runtime.
        /// </summary>
        public SubstanceGraph runtimeGraph
        {
            get
            {
                if (mode == SubstanceRandomizationMode.Graph)
                {
                    return m_SubstanceGraph;
                }

                InitializeInstance();
                return m_InternalGraph;
            }
            set
            {
                m_SubstanceGraph = value;
                m_Initialized = false;
            }
        }

        /// <summary>
        /// When <see cref="mode" /> is "Graph," <see cref="m_Initialized" /> should be always true.
        /// When <see cref="mode" /> is "Instance," <see cref="m_Initialized" /> is true if and only if a copy of
        /// <see cref="m_SubstanceGraph" /> has been made and a reference to it has been stored in <see cref="m_InternalGraph" />.
        /// </summary>
        void InitializeInstance()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;
            if (mode == SubstanceRandomizationMode.Instance)
            {
                m_InternalGraph = m_SubstanceGraph.Duplicate();
                GetComponent<Renderer>().sharedMaterial = m_InternalGraph.material;
            }
        }

        void Start()
        {
            var theRenderer = GetComponent<Renderer>();
            // Find which material element has the material associated with the substance graph above
            m_SubstanceGraphMaterialIndex = -1;
            for (var i = 0; i < theRenderer.sharedMaterials.Length; i++)
            {
                if (theRenderer.sharedMaterials[i] == m_SubstanceGraph.material)
                    m_SubstanceGraphMaterialIndex = i;
            }
            InitializeInstance();
        }
    }
}
#endif
