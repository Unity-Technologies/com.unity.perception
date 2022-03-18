using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces 3d bounding box ground truth for all visible and <see cref="Labeling"/> objects each frame.
    /// </summary>
    /// <remarks>The BoundingBox3DLabeler does not support <see cref="SkinnedMeshRenderer"/> objects, they will be ignored.</remarks>
    public class BoundingBox3DLabeler : CameraLabeler
    {
        ///<inheritdoc/>
        public override string description => BoundingBox3DDefinition.labelerDescription;

        // ReSharper disable MemberCanBePrivate.Global
        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "bounding box 3D";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;
        // ReSharper restore MemberCanBePrivate.Global

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxes3DReceived");
        BoundingBox3DDefinition m_Definition;

        Dictionary<int, AsyncFuture<Annotation>> m_AsyncAnnotations;
        Dictionary<int, Dictionary<uint, BoundingBox3D>> m_BoundingBoxValues;

        List<BoundingBox3D> m_LatestReported;

        int m_CurrentFrame;


        /// <summary>
        /// Color to use for 3D visualization box
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public Color visualizationColor = Color.green;

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <summary>
        /// Fired when the bounding boxes are computed for a frame.
        /// </summary>
        public event Action<int, List<BoundingBox3D>> BoundingBoxComputed;

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
            idLabelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox3DLabeler's idLabelConfig field must be assigned");

            m_Definition = new BoundingBox3DDefinition(annotationId, idLabelConfig.GetAnnotationSpecification());

            DatasetCapture.RegisterAnnotationDefinition(m_Definition);

            perceptionCamera.RenderedObjectInfosCalculated += OnRenderObjectInfosCalculated;

            m_AsyncAnnotations = new Dictionary<int, AsyncFuture<Annotation>>();
            m_BoundingBoxValues = new Dictionary<int, Dictionary<uint, BoundingBox3D>>();
            visualizationEnabled = supportsVisualization;
        }

        static BoundingBox3D ConvertToBoxData(IdLabelEntry label, uint instanceId, Vector3 center, Vector3 extents, Quaternion rot)
        {
            return new BoundingBox3D
            {
                labelId = label.id,
                labelName = label.label,
                instanceId = instanceId,
                translation = center,
                size = extents * 2,
                rotation = rot,
                acceleration = Vector3.zero,
                velocity = Vector3.zero
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

        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_CurrentFrame = Time.frameCount;

            m_BoundingBoxValues[m_CurrentFrame] = new Dictionary<uint, BoundingBox3D>();

            m_AsyncAnnotations[m_CurrentFrame] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_Definition);

            foreach (var label in LabelManager.singleton.registeredLabels)
                ProcessLabel(label);
        }

        void OnRenderObjectInfosCalculated(int frameCount, NativeArray<RenderedObjectInfo> renderedObjectInfos)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var asyncAnnotation))
                return;

            if (!m_BoundingBoxValues.TryGetValue(frameCount, out var boxes))
            {
                Debug.LogError($"Could not find a 3D bounding box for frame: {asyncAnnotation.pendingId}");
                return;
            }


            m_AsyncAnnotations.Remove(frameCount);
            m_BoundingBoxValues.Remove(frameCount);

            using (s_BoundingBoxCallback.Auto())
            {
                var reportList = new List<BoundingBox3D>();

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];

                    if (boxes.TryGetValue(objectInfo.instanceId, out var box))
                    {
                        reportList.Add(box);
                    }
                }

                BoundingBoxComputed?.Invoke(frameCount, reportList);

                var toReport = new BoundingBox3DAnnotation(m_Definition, perceptionCamera.ID, reportList);
                asyncAnnotation.Report(toReport);
                m_LatestReported = reportList;
            }
        }

        void ProcessLabel(Labeling labeledEntity)
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
                // Currently we are only reporting objects that are a) labeled and b) are visible based on the perception
                // camera's rendered object info. In the future we plan on reporting how much of the object can be seen, including
                // none if it is off camera
                if (idLabelConfig.TryGetLabelEntryFromInstanceId(labeledEntity.instanceId, out var labelEntry))
                {
                    var entityGameObject = labeledEntity.gameObject;

                    var meshFilters = entityGameObject.GetComponentsInChildren<MeshFilter>();
                    if (meshFilters == null || meshFilters.Length == 0) return;

                    var labelTransform = entityGameObject.transform;
                    var cameraTransform = perceptionCamera.transform;
                    var combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
                    var areBoundsUnset = true;

                    // Need to convert all bounds into labeling mesh space...
                    foreach (var mesh in meshFilters)
                    {
                        if (!mesh.GetComponent<Renderer>().enabled)
                            continue;

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
                            var localScale = currentTransform.localScale;
                            var localRotation = currentTransform.localRotation;

                            transformedBounds.center = Vector3.Scale(transformedBounds.center, localScale);
                            transformedBounds.center = localRotation * transformedBounds.center;
                            transformedBounds.center += currentTransform.localPosition;
                            transformedBounds.extents = Vector3.Scale(transformedBounds.extents, localScale);
                            transformedRotation *= localRotation;
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
                    combinedBounds.extents = Vector3.Scale(combinedBounds.extents,  labelTransform.lossyScale);

                    var camRotation = cameraTransform.rotation;
                    // Now adjust the center and rotation to camera space. Camera space transforms never rescale objects
                    combinedBounds.center -= cameraTransform.position;
                    combinedBounds.center = Quaternion.Inverse(camRotation) * combinedBounds.center;
                    var cameraRotation = Quaternion.Inverse(camRotation) * labelTransform.rotation;

                    var converted = ConvertToBoxData(labelEntry, labeledEntity.instanceId, combinedBounds.center, combinedBounds.extents, cameraRotation);

                    m_BoundingBoxValues[m_CurrentFrame][labeledEntity.instanceId] = converted;
                }
            }
        }

        static Vector3 CalculateRotatedPoint(Camera cam, Vector3 start, Vector3 xDirection, Vector3 yDirection, Vector3 zDirection, float xScalar, float yScalar, float zScalar)
        {
            var transform = cam.transform;
            var rotatedPoint = start + xDirection * xScalar + yDirection * yScalar + zDirection * zScalar;
            var worldPoint = transform.position + transform.rotation * rotatedPoint;
            return VisualizationHelper.ConvertToScreenSpace(cam, worldPoint);
        }

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            if (m_LatestReported == null) return;

            var cam = perceptionCamera.attachedCamera;

            foreach (var box in m_LatestReported)
            {
                var t = box.translation;

                var right = box.rotation * Vector3.right;
                var up = box.rotation * Vector3.up;
                var forward = box.rotation * Vector3.forward;

                var s = box.size * 0.5f;
                var bbl = CalculateRotatedPoint(cam, t,right, up, forward,-s.x,-s.y, -s.z);
                var btl = CalculateRotatedPoint(cam, t,right, up, forward,-s.x, s.y, -s.z);
                var btr = CalculateRotatedPoint(cam, t,right, up, forward,s.x, s.y, -s.z);
                var bbr = CalculateRotatedPoint(cam, t,right, up, forward,s.x, -s.y, -s.z);

                VisualizationHelper.DrawLine(bbl, btl, visualizationColor);
                VisualizationHelper.DrawLine(bbl, bbr, visualizationColor);
                VisualizationHelper.DrawLine(btr, btl, visualizationColor);
                VisualizationHelper.DrawLine(btr, bbr, visualizationColor);

                var fbl = CalculateRotatedPoint(cam, t,right, up, forward,-s.x,-s.y, s.z);
                var ftl = CalculateRotatedPoint(cam, t,right, up, forward,-s.x, s.y, s.z);
                var ftr = CalculateRotatedPoint(cam, t,right, up, forward,s.x, s.y, s.z);
                var fbr = CalculateRotatedPoint(cam, t,right, up, forward,s.x, -s.y, s.z);

                VisualizationHelper.DrawLine(fbl, ftl, visualizationColor);
                VisualizationHelper.DrawLine(fbl, fbr, visualizationColor);
                VisualizationHelper.DrawLine(ftr, ftl, visualizationColor);
                VisualizationHelper.DrawLine(ftr, fbr, visualizationColor);

                VisualizationHelper.DrawLine(fbl, bbl, visualizationColor);
                VisualizationHelper.DrawLine(fbr, bbr, visualizationColor);
                VisualizationHelper.DrawLine(ftl, btl, visualizationColor);
                VisualizationHelper.DrawLine(ftr, btr, visualizationColor);

            }
        }
    }
}
