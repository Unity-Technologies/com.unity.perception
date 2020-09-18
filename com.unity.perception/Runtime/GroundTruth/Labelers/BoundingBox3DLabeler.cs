using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Entities;
using Unity.Profiling;

namespace UnityEngine.Perception.GroundTruth
{


    /// <summary>
    /// Produces 3d bounding box ground truth for all visible and <see cref="Labeling"/> objects each frame.
    /// </summary>
    public class BoundingBox3DLabeler : CameraLabeler, IGroundTruthUpdater
    {
        ///<inheritdoc/>
        public override string description
        {
            get => "Produces 3D bounding box ground truth data for all visible objects that bear a label defined in this labeler's associated label configuration.";
            protected set {}
        }

        // ReSharper disable MemberCanBePrivate.Global
        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "0bfbe00d-00fa-4555-88d1-471b58449f5c";
        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;
        // ReSharper restore MemberCanBePrivate.Global

        /// <summary>
        /// Each 3D bounding box data record maps a tuple of (instance, label) to translation, size and rotation that draws a 3D bounding box,
        /// as well as velocity and acceleration (optional) of the 3D bounding box. All location data is given with respect to the sensor coordinate system.
        ///
        /// bounding_box_3d
        ///      label_id (int): Integer identifier of the label
        ///      label_name (str): String identifier of the label
        ///      instance_id (str): UUID of the instance.
        ///      translation (float, float, float): 3d bounding box's center location in meters as center_x, center_y, center_z with respect to global coordinate system.
        ///      size (float, float, float): 3d bounding box size in meters as width, length, height.
        ///      rotation (float, float, float, float): 3d bounding box orientation as quaternion: w, x, y, z.
        ///      velocity (float, float, float) [optional]: 3d bounding box velocity in meters per second as v_x, v_y, v_z.
        ///      acceleration (float, float, float) [optional]: 3d bounding box acceleration in meters per second^2 as a_x, a_y, a_z.
        /// </summary>
        /// <remarks>
        /// Currently not supporting exporting velocity and acceleration. Both values will be null.
        /// </remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class BoxData
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
            public float[] translation;
            public float[] size;
            public float[] rotation;
            public float[] velocity; // TODO
            public float[] acceleration; // TODO
        }

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxes3DReceived");
        AnnotationDefinition m_AnnotationDefinition;
        List<BoxData> m_BoundingBoxValues;

        int m_CurrentFrame;

        /// <inheritdoc/>
        protected override bool supportsVisualization => false;

        /// <summary>
        /// Fired when the bounding boxes are computed for a frame.
        /// </summary>
        public event Action<int, BoxData> BoundingBoxComputed;

        /// <summary>
        /// Creates a new BoundingBox3DLabeler. Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public BoundingBox3DLabeler() {}

        /// <summary>
        /// Creates a new BoundingBox3DLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config for resolving the label for each object.</param>
        public BoundingBox3DLabeler(IdLabelConfig labelConfig)
        {
            this.idLabelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox2DLabeler's idLabelConfig field must be assigned");

            var updater = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystem<GroundTruthUpdateSystem>();
            updater?.Activate(this);

            m_AnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box 3D", idLabelConfig.GetAnnotationSpecification(),
                "Bounding box for each labeled object visible to the sensor", id: new Guid(annotationId));
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            var updater = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthUpdateSystem>();
            updater?.Deactivate(this);
        }

        /// <inheritdoc/>
        public void OnBeginUpdate()
        {
            if (m_BoundingBoxValues == null)
                m_BoundingBoxValues = new List<BoxData>();
            else
                m_BoundingBoxValues.Clear();

            m_CurrentFrame = Time.frameCount;
        }

        static BoxData ConvertToBoxData(IdLabelEntry label, uint instanceId, Vector3 center, Vector3 extents, Quaternion rotation)
        {
            return new BoxData
            {
                label_id = label.id,
                label_name = label.label,
                instance_id = instanceId,
                translation = new[] { center.x, center.y, center.z },
                size = new[] { extents.x * 2, extents.y * 2, extents.z * 2 },
                rotation = new[] { rotation.x, rotation.y, rotation.z, rotation.w },
                velocity = null,
                acceleration = null
            };
        }

        /// <inheritdoc/>
        public void OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                // Grab all of the mesh filters that may be contained in the game object
                var meshFilters = labeling.gameObject.GetComponentsInChildren<MeshFilter>();
                if (meshFilters == null || meshFilters.Length == 0) return;

                if (idLabelConfig.TryGetLabelEntryFromInstanceId(groundTruthInfo.instanceId, out var labelEntry))
                {
                    var camTrans = perceptionCamera.transform;
                    var bounds = new Bounds(Vector3.zero, Vector3.zero);
                    var boundsUnset = true;

                    // Go through each sub mesh and convert its bounds into the labeling meshes space
                    foreach (var mesh in meshFilters)
                    {
                        var currentTransform = mesh.gameObject.transform;

                        // Need to copy the bounds because we are going to move them, and do not want to change
                        // the bounds of the mesh
                        var meshBounds = mesh.mesh.bounds;
                        var transformedBounds = new Bounds(meshBounds.center, meshBounds.size);

                        // Convert all sub-components bounds into top level component's space
                        while (currentTransform != labeling.transform)
                        {
                            transformedBounds.center += currentTransform.localPosition;
                            transformedBounds.extents = currentTransform.TransformVector(transformedBounds.extents);
                            currentTransform = currentTransform.parent;
                        }

                        // Merge the bounds of the current child with the the entire component's bounds
                        if (boundsUnset)
                        {
                            bounds.center = transformedBounds.center;
                            bounds.extents = transformedBounds.extents;
                            boundsUnset = false;
                        }
                        else
                            bounds.Encapsulate(transformedBounds);
                    }

                    // Convert the encapsulated model's bounds into world space
                    var labelTransform = labeling.transform;
                    bounds.center = labelTransform.TransformPoint(bounds.center);
                    bounds.extents = Vector3.Scale(bounds.extents,  labelTransform.localScale);

                    // Now convert all points into camera's space
                    var localRotation = Quaternion.Inverse(camTrans.rotation) * labelTransform.rotation;
                    var localCenter = camTrans.InverseTransformPoint(bounds.center);
                    localCenter = Vector3.Scale(camTrans.localScale, localCenter);

                    var converted = ConvertToBoxData(labelEntry, groundTruthInfo.instanceId, localCenter, bounds.extents, localRotation);
                    BoundingBoxComputed?.Invoke(m_CurrentFrame, converted);
                    m_BoundingBoxValues.Add(converted);
                }
            }
        }

        /// <inheritdoc/>
        public void OnEndUpdate()
        {
            perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition).ReportValues(m_BoundingBoxValues);
        }
    }
}
