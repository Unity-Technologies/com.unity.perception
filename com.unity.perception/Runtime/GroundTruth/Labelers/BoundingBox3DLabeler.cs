using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
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

        Plane[] m_PerceptionCameraFrustumFrames;
        
        /// <inheritdoc/>
        public void OnBeginUpdate()
        {
            if (m_BoundingBoxValues == null)
                m_BoundingBoxValues = new List<BoxData>();
            else
                m_BoundingBoxValues.Clear();

            m_PerceptionCameraFrustumFrames = GeometryUtility.CalculateFrustumPlanes(perceptionCamera.attachedCamera);
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

        static Vector3[] GetBoxCorners(Bounds bounds, Quaternion rotation)
        {
            var boundsCenter = bounds.center;
            var right = Vector3.right * bounds.extents.x;
            var up = Vector3.up * bounds.extents.y;
            var forward = Vector3.forward * bounds.extents.z;

            right = rotation * right;
            up = rotation * up;
            forward = rotation * forward;
            
            var doubleRight = right * 2;
            var doubleUp = up * 2;
            var doubleForward = forward * 2;
            
            var corners = new Vector3[8];
            corners[0] = boundsCenter - right - up - forward;
            corners[1] = corners[0] + doubleUp;
            corners[2] = corners[1] + doubleRight;
            corners[3] = corners[0] + doubleRight; 
            for (var i = 0; i < 4; i++)
            {
                corners[i + 4] = corners[i] + doubleForward;
            }

            return corners;
        }

        bool IsInCameraView(GameObject target)
        {
            // Need to check against all of the meshes in the labeled object, if at least one is visible, then
            // the object will be considered in the camera's view. The built in Unity geometry methods are optimized to
            // work with AABB bounds, so we will use those from the mesh renderer, as opposed to the non-axis aligned
            // bounding boxes retrieved from the mesh filter. There is a chance that this method can report false
            // positives, but it is (at least right now) probably worth the trade off for performance

            var renderers = target.GetComponentsInChildren<Renderer>();
            return renderers.Any(r => GeometryUtility.TestPlanesAABB(m_PerceptionCameraFrustumFrames, r.bounds));
        }

        /// <inheritdoc/>
        public void OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                // Unfortunately to get the non-axis aligned bounding prism from a game object is not very
                // straightforward. A game object's default bounding prism is always axis aligned. To find a "tight"
                // fitting prism for a game object we must calculate the oriented bounds of all of the meshes in a
                // game object. These meshes (in the object tree) may go through a series of transformations. We need
                // to transform all of the children mesh bounds into the coordinate space of the "labeled" game object
                // and then intersect all of those bounds together. We then need to apply the "labeled" game object's
                // transform to the combined bounds to transform the bounds into world space. Finally, we then need
                // to take the bounds in world space and transform it to camera space to record it to json...
                //
                // Currently we are only reporting objects that are a) labeled and b) are inside of the view camera's
                // frustum. In the future we plan on supporting to report how much of the object can be seen, including
                // none if it is off camera
                if (idLabelConfig.TryGetLabelEntryFromInstanceId(groundTruthInfo.instanceId, out var labelEntry))
                {
                    if (!IsInCameraView(labeling.gameObject)) return;
                    
                    var meshFilters = labeling.gameObject.GetComponentsInChildren<MeshFilter>();
                    if (meshFilters == null || meshFilters.Length == 0) return;
                    
                    var labelTransform = labeling.transform;
                    var cameraTransform = perceptionCamera.transform;
                    var combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
                    var areBoundsUnset = true;

                    // Need to convert all bounds into labeling mesh space...
                    foreach (var mesh in meshFilters)
                    {
                        var currentTransform = mesh.gameObject.transform;
                        // Grab the bounds of the game object from the mesh, although these bounds are axis-aligned,
                        // they are axis-aligned with respect to the current component's coordinate space. This, in theory
                        // could still provide non-ideal fitting bounds (if the model is made strangely, but garbage in; garbage out)
                        var meshBounds = mesh.mesh.bounds;
                        
                        var transformedBounds = new Bounds(meshBounds.center, meshBounds.size);
                        var transformedRotation = Quaternion.identity;
                        
                        // Apply the transformations on this object until we reach the labeled transform
                        while (currentTransform != labelTransform)
                        {
                            transformedBounds.center += currentTransform.localPosition;
                            transformedBounds.extents = Vector3.Scale(transformedBounds.extents, currentTransform.localScale);
                            transformedRotation *= currentTransform.localRotation;
                            currentTransform = currentTransform.parent;
                        }

                        // Due to rotations that may be applied, we cannot simply use the extents of the bounds, but
                        // need to calculate all 8 corners of the bounds and combine them with the current combined
                        // bounds
                        var corners = GetBoxCorners(transformedBounds, transformedRotation);
                        
                        // If this is the first time, create a new bounds struct
                        if (areBoundsUnset)
                        {
                            combinedBounds = new Bounds(corners[0], Vector3.zero);
                            areBoundsUnset = false;
                        }

                        // Go through each corner add add it to the bounds
                        foreach (var c2 in corners)
                        {
                            combinedBounds.Encapsulate(c2);
                        }
                    }
                    
                    // Convert the combined bounds into world space
                    combinedBounds.center = labelTransform.TransformPoint(combinedBounds.center);
                    combinedBounds.extents = Vector3.Scale(combinedBounds.extents,  labelTransform.localScale);

                    // Now convert all points into camera's space
                    var cameraCenter = cameraTransform.InverseTransformPoint(combinedBounds.center);
                    cameraCenter = Vector3.Scale(cameraTransform.localScale, cameraCenter);
                    
                    // Rotation to go from label space to camera space
                    var cameraRotation = Quaternion.Inverse(cameraTransform.rotation) * labelTransform.rotation;
                    
                    var converted = ConvertToBoxData(labelEntry, groundTruthInfo.instanceId, cameraCenter, combinedBounds.extents, cameraRotation);
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
