using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Produces 3d bounding box ground truth for all visible and <see cref="Labeling"/> objects each frame.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
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

        class PendingFrame
        {
            public AsyncFuture<Annotation> asyncAnnotation;
            public Dictionary<uint, (bool pending, bool visible, BoundingBox3D bb)> pendingObjectsByInstanceId = new();
            public int remainingPendingObjects;
            public bool visibilityCheckComplete = false;
        }

        Dictionary<int, PendingFrame> m_PendingFrames;
        List<BoundingBox3D> m_LatestReported;
        int m_CurrentFrame;

        /// <summary>
        /// Color to use for 3D visualization box
        /// </summary>
        public Color visualizationColor = Color.green;

        ComputeShader m_Calculate3DBoundingBox;
        int3 m_ThreadGroupSizes;

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

            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderObjectInfosCalculated;
            m_PendingFrames = new Dictionary<int, PendingFrame>();
            visualizationEnabled = supportsVisualization;
            m_Calculate3DBoundingBox = ComputeUtilities.LoadShader("Calculate3DBoundingBox");
        }

        protected override void OnUpdate()
        {
            foreach (var label in LabelManager.singleton.registeredLabels)
            {
                var skinnedMeshRenderers = label.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in skinnedMeshRenderers)
                {
                    //set vertexBufferTarget here so that it is available in endCameraRendering
                    renderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_CurrentFrame = Time.frameCount;

            var pendingData = new PendingFrame();
            m_PendingFrames[m_CurrentFrame] = pendingData;
            pendingData.asyncAnnotation = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_Definition);

            foreach (var label in LabelManager.singleton.registeredLabels)
                ProcessLabel(label, scriptableRenderContext, pendingData);
        }

        void OnRenderObjectInfosCalculated(
            int frameCount,
            NativeArray<RenderedObjectInfo> renderedObjectInfos,
            SceneHierarchyInformation hierarchyInfo
        )
        {
            if (!m_PendingFrames.TryGetValue(frameCount, out var pendingData))
                return;

            //filter to only visible objects
            using (s_BoundingBoxCallback.Auto())
            {
                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];

                    if (pendingData.pendingObjectsByInstanceId.TryGetValue(objectInfo.instanceId, out var box))
                    {
                        box.visible = true;
                        pendingData.pendingObjectsByInstanceId[objectInfo.instanceId] = box;
                    }
                }

                pendingData.visibilityCheckComplete = true;

                ReportIfComplete(frameCount);
            }
        }

        void ReportIfComplete(int frameCount)
        {
            var pendingData = m_PendingFrames[frameCount];
            if (!pendingData.visibilityCheckComplete || pendingData.remainingPendingObjects != 0)
                return;

            m_PendingFrames.Remove(frameCount);
            var reportList = pendingData.pendingObjectsByInstanceId
                .Where(v => v.Value.visible)
                .Select(v => v.Value.bb).ToList();
            BoundingBoxComputed?.Invoke(frameCount, reportList);

            var toReport = new BoundingBox3DAnnotation(m_Definition, perceptionCamera.id, reportList);
            pendingData.asyncAnnotation.Report(toReport);
            m_LatestReported = reportList;
        }

        class PendingBounds
        {
            public uint instanceId;
            public BoundingBox3D boundingBox3D;
            public Vector3 labelObjectScale;
            public Vector3 labeledObjectToCameraPosition;
            public Quaternion cameraRotation;
            public Bounds? bounds;
            public int pendingChildBoundsCount;
            public int frame;
            public Matrix4x4 labeledObjectToCameraMatrix;
        }
        Bounds CombineBounds(Bounds? bounds, Bounds meshBounds)
        {
            // If this is the first time, create a new bounds struct
            if (!bounds.HasValue)
                bounds = meshBounds;
            else
            {
                var newBounds = bounds.Value;
                newBounds.Encapsulate(meshBounds);
                bounds = newBounds;
            }

            return bounds.Value;
        }

        bool ProcessMeshFiltersAsync(MeshFilter[] meshFilters, SkinnedMeshRenderer[] skinnedMeshRenderers, Labeling labeledEntity, PendingBounds pendingBounds,
            ScriptableRenderContext context)
        {
            var entityGameObject = labeledEntity.gameObject;
            var labelTransform = entityGameObject.transform;
            bool any = false;

            // Compute the object bounds on the GPU using the vertices of the mesh transformed into the space of the labeled object
            foreach (var mesh in meshFilters)
            {
                if (!mesh.GetComponent<Renderer>().enabled)
                    continue;

                mesh.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                var objectToLabelSpaceTransform =
                    labelTransform.worldToLocalMatrix * mesh.transform.localToWorldMatrix;

                var graphicsBuffer = mesh.mesh.GetVertexBuffer(0);
                Compute3DbbAsync(context, graphicsBuffer, objectToLabelSpaceTransform, pendingBounds);
                graphicsBuffer.Dispose();
                any = true;
            }
            foreach (var mesh in skinnedMeshRenderers)
            {
                if (!mesh.GetComponent<Renderer>().enabled)
                    continue;

                var rootBoneLocalToWorldMatrix = mesh.rootBone.localToWorldMatrix;
                var objectToLabelSpaceTransform =
                    labelTransform.worldToLocalMatrix *
                    rootBoneLocalToWorldMatrix *
                    //skinnedMeshRenderer's scale is baked into its vertex buffer
                    Matrix4x4.Scale(rootBoneLocalToWorldMatrix.lossyScale).inverse;

                var graphicsBuffer = mesh.GetVertexBuffer();
                Compute3DbbAsync(context, graphicsBuffer, objectToLabelSpaceTransform, pendingBounds);
                graphicsBuffer.Dispose();
                any = true;
            }

            return any;
        }

        // Calculates the 3d bounding box of the given vertices on the GPU using a reduction algorithm in a compute shader
        void Compute3DbbAsync(ScriptableRenderContext scriptableRenderContext, GraphicsBuffer vertexBuffer,
            Matrix4x4 rootToLabeledObjectTransform, PendingBounds pendingBounds)
        {
            m_ThreadGroupSizes = ComputeUtilities.GetKernelThreadGroupSizes(m_Calculate3DBoundingBox, 0);
            var commandBuffer = CommandBufferPool.Get("BoundingBox3D for SkinnedMeshRenderer");
            commandBuffer.SetComputeMatrixParam(m_Calculate3DBoundingBox, "gRootToLabeledObjectTransform", rootToLabeledObjectTransform);
            commandBuffer.SetComputeBufferParam(m_Calculate3DBoundingBox, 0, "vertexBuffer", vertexBuffer);
            commandBuffer.SetComputeIntParam(m_Calculate3DBoundingBox, "gStrideBytes", vertexBuffer.stride);
            //round up to the nearest set of groups
            var threadGroups = ComputeUtilities.ThreadGroupsCount(vertexBuffer.count, m_ThreadGroupSizes.x);
            var resultBuffer = new ComputeBuffer(threadGroups * 2, 3 * sizeof(float));
            commandBuffer.SetComputeBufferParam(m_Calculate3DBoundingBox, 0, "bb3dOut", resultBuffer);
            commandBuffer.SetComputeBufferParam(m_Calculate3DBoundingBox, 1, "bb3dOut", resultBuffer);
            commandBuffer.SetComputeIntParam(m_Calculate3DBoundingBox, "gVertexCount", vertexBuffer.count);

            //Dispatch one to reduce from vertices to bounding boxes. This will result in N bounding box, where N is
            //the # of thread groups
            commandBuffer.DispatchCompute(m_Calculate3DBoundingBox, 0, threadGroups, 1, 1);

            //reduce the bounding boxes calculated in the first pass multiple times until we reduce all AABBs to a single AABB
            while (threadGroups > 1)
            {
                threadGroups = ComputeUtilities.ThreadGroupsCount(threadGroups, m_ThreadGroupSizes.x);
                commandBuffer.DispatchCompute(m_Calculate3DBoundingBox, 1, threadGroups, 1, 1);
            }

            pendingBounds.pendingChildBoundsCount++;
            commandBuffer.RequestAsyncReadback(resultBuffer,
                request =>
                {
                    var result = request.GetData<Vector3>();
                    resultBuffer.Release();
                    if (request.hasError)
                        Debug.LogError("Error reading back bounding box from compute buffer");

                    var minPosition = result[0];
                    var maxPosition = result[1];
                    var bounds = new Bounds((maxPosition + minPosition) / 2, maxPosition - minPosition);

                    pendingBounds.bounds = CombineBounds(pendingBounds.bounds, bounds);
                    pendingBounds.pendingChildBoundsCount--;
                    if (pendingBounds.pendingChildBoundsCount == 0)
                        CommitBoundingBox(pendingBounds);
                });
            scriptableRenderContext.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        void ProcessLabel(Labeling labeledEntity, ScriptableRenderContext scriptableRenderContext,
            PendingFrame pendingFrame)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                // Unfortunately to get the non-axis aligned bounding prism from a game object is not very
                // straightforward. A game object's default bounding prism is always axis aligned. To find a "tight"
                // fitting prism for a game object we must calculate the oriented bounds of all of the meshes in a
                // game object. These meshes (in the object tree) may go through a series of transformations.
                //
                // Currently we are only reporting objects that are a) labeled and b) are visible based on the perception
                // camera's rendered object info.
                if (!idLabelConfig.TryGetLabelEntryFromInstanceId(labeledEntity.instanceId, out var labelEntry))
                    return;

                var entityGameObject = labeledEntity.gameObject;

                var skinnedMeshRenderers = entityGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                var meshFilters = entityGameObject.GetComponentsInChildren<MeshFilter>();

                if ((meshFilters == null || meshFilters.Length == 0) &&
                    (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0))
                    return;

                var labelTransform = labeledEntity.transform;
                var cameraTransform = perceptionCamera.transform;

                // Convert the combined bounds into world space
                var rotationToCameraSpace = Quaternion.Inverse(cameraTransform.rotation) * labelTransform.rotation;

                var converted = new BoundingBox3D
                {
                    labelId = labelEntry.id,
                    labelName = labelEntry.label,
                    instanceId = labeledEntity.instanceId,
                    translation = Vector3.negativeInfinity, //will be filled in later
                    size = Vector3.negativeInfinity,
                    rotation = rotationToCameraSpace,
                    acceleration = Vector3.zero,
                    velocity = Vector3.zero
                };

                var pendingBounds = new PendingBounds()
                {
                    instanceId = labeledEntity.instanceId,
                    frame = m_CurrentFrame,
                    labeledObjectToCameraMatrix = Matrix4x4.Scale(cameraTransform.worldToLocalMatrix.lossyScale).inverse *
                        cameraTransform.worldToLocalMatrix *
                        labelTransform.localToWorldMatrix,
                    boundingBox3D = converted,
                    bounds = null
                };

                if (!ProcessMeshFiltersAsync(
                    meshFilters, skinnedMeshRenderers, labeledEntity, pendingBounds, scriptableRenderContext))
                    return;

                pendingFrame.remainingPendingObjects++;
                pendingFrame.pendingObjectsByInstanceId[labeledEntity.instanceId] = (true, false, converted);
            }
        }

        void CommitBoundingBox(PendingBounds pendingBounds)
        {
            var objectLocalBounds = pendingBounds.bounds.Value;
            var cameraLocalBounds = BoundsFromObjectToCameraSpace(
                pendingBounds.labeledObjectToCameraMatrix,
                objectLocalBounds);

            pendingBounds.bounds = cameraLocalBounds;
            pendingBounds.boundingBox3D.size = cameraLocalBounds.size;
            pendingBounds.boundingBox3D.translation = cameraLocalBounds.center;

            var pendingData = m_PendingFrames[pendingBounds.frame];
            var boundingBoxInfo = pendingData.pendingObjectsByInstanceId[pendingBounds.instanceId];
            boundingBoxInfo.pending = false;
            boundingBoxInfo.bb = pendingBounds.boundingBox3D;
            pendingData.remainingPendingObjects--;
            Debug.Assert(pendingData.remainingPendingObjects >= 0, "remainingPendingBounds should not be < 0");

            pendingData.pendingObjectsByInstanceId[pendingBounds.instanceId] = boundingBoxInfo;

            ReportIfComplete(pendingBounds.frame);
        }

        static Bounds BoundsFromObjectToCameraSpace(Matrix4x4 localToCameraTransform, Bounds bounds)
        {
            // Now adjust the center and rotation to camera space. Camera space transforms never rescale objects
            bounds.center = localToCameraTransform.MultiplyPoint(bounds.center);
            bounds.extents = Vector3.Scale(localToCameraTransform.lossyScale, bounds.extents);
            return bounds;
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
                var bbl = CalculateRotatedPoint(cam, t, right, up, forward, -s.x, -s.y, -s.z);
                var btl = CalculateRotatedPoint(cam, t, right, up, forward, -s.x, s.y, -s.z);
                var btr = CalculateRotatedPoint(cam, t, right, up, forward, s.x, s.y, -s.z);
                var bbr = CalculateRotatedPoint(cam, t, right, up, forward, s.x, -s.y, -s.z);

                VisualizationHelper.DrawLine(bbl, btl, visualizationColor);
                VisualizationHelper.DrawLine(bbl, bbr, visualizationColor);
                VisualizationHelper.DrawLine(btr, btl, visualizationColor);
                VisualizationHelper.DrawLine(btr, bbr, visualizationColor);

                var fbl = CalculateRotatedPoint(cam, t, right, up, forward, -s.x, -s.y, s.z);
                var ftl = CalculateRotatedPoint(cam, t, right, up, forward, -s.x, s.y, s.z);
                var ftr = CalculateRotatedPoint(cam, t, right, up, forward, s.x, s.y, s.z);
                var fbr = CalculateRotatedPoint(cam, t, right, up, forward, s.x, -s.y, s.z);

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
