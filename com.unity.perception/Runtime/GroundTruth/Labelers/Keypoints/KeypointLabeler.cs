using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Produces keypoint annotations for a humanoid model. This labeler supports generic
    /// <see cref="KeypointTemplate"/>. Template values are mapped to rigged
    /// <see cref="Animator"/> <seealso cref="Avatar"/>. Custom joints can be
    /// created by applying <see cref="JointLabel"/> to empty game objects at a body
    /// part's location.
    ///
    /// Keypoints are recorded by this labeler with a state value describing if they are present on the model,
    /// present but not visible, or visible. A keypoint can be listed as not visible for three reasons: it is outside
    /// of the camera's view frustum, it is occluded by another object in the scene, or it is occluded by itself, for
    /// example a raised arm in front a model's face could occlude its eyes from being visible. To calculate self
    /// occlusion values, the keypoint labeler uses tolerances per keypoint to determine if the keypoint is blocked.
    /// The initial tolerance value for each keypoint is set per keypoint in the <see cref="KeypointTemplate"/> file.
    /// The tolerance of a custom keypoints can be set with the <see cref="JointLabel"/> used to create the keypoint.
    /// Finally, a <see cref="KeypointOcclusionOverrides"/> component be added to a model to apply a universal scaling
    /// override to all of the keypoint tolerances defined in a keypoint template.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class KeypointLabeler : CameraLabeler
    {
        struct FrameKeypointData
        {
            public AsyncFuture<Annotation> annotation;
            public int pointsPerEntry;
            public List<KeypointComponent> keypoints;
            public bool isDepthCheckComplete;
            public bool isInstanceSegmentationCheckComplete;
            public NativeArray<RenderedObjectInfo> objectInfos;
        }

        // Smaller texture sizes produce assertion failures in the engine
        const int k_MinTextureWidth = 8;
        const int k_PixelTolerance = 1;

        static readonly int k_KeypointPositions = Shader.PropertyToID("_KeypointPositions");
        static readonly int k_KeypointDepthToCheck = Shader.PropertyToID("_KeypointDepthToCheck");
        static readonly int k_LinearDepthTexture = Shader.PropertyToID("_LinearDepthTexture");
        static readonly int k_CameraPixelHeight = Shader.PropertyToID("_CameraPixelHeight");
        static readonly int k_CameraFarPlane = Shader.PropertyToID("_CameraFarPlane");

        static ProfilerMarker s_OnEndRenderingMarker = new("KeypointLabeler OnEndRendering");
        static ProfilerMarker s_OnVisualizeMarker = new("KeypointLabeler OnVisualize");

        int m_CurrentFrame;
        Material m_DepthCheckMaterial;
        AnnotationDefinition m_AnnotationDefinition;
        Texture2D m_MissingTexture;
        Texture2D m_KeypointPositionsTexture;
        Texture2D m_KeypointCheckDepthTexture;
        RenderTexture m_InstanceIdTexture;
        RenderTexture m_DepthTexture;
        RenderTexture m_ResultsBuffer;
        Dictionary<int, FrameKeypointData> m_FrameKeypointData = new();
        Dictionary<int, NativeArray<uint>> m_CachedInstanceIds = new();
        List<KeypointComponent> m_LatestReported;

        /// <summary>
        /// The active keypoint template. Required to annotate keypoint data.
        /// </summary>
        public KeypointTemplate activeTemplate;

        /// <inheritdoc/>
        public override string description => KeypointAnnotationDefinition.labelerDescription;

        ///<inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "keypoints";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;

        /// <summary>
        /// Should the visualizer draw occluded points.
        /// </summary>
        [Tooltip("If checked on the visualizer will draw an empty black circle for occluded points. If unchecked the point will not be drawn.")]
        public bool visualizeOccludedPoints = true;

        /// <summary>
        /// Controls which objects will have keypoints recorded in the dataset.
        /// <see cref="KeypointObjectFilter"/>
        /// </summary>
        public KeypointObjectFilter objectFilter;

        /// <summary>
        /// Array of animation pose labels which map animation clip times to ground truth pose labels.
        /// </summary>
        public List<AnimationPoseConfig> animationPoseConfigs;

        /// <summary>
        /// Action that gets triggered when a new frame of key points are computed.
        /// </summary>
        public event Action<int, List<KeypointComponent>> KeypointsComputed;

        /// <summary>
        /// Creates a new key point labeler. This constructor creates a labeler that
        /// is not valid until a <see cref="IdLabelConfig"/> and <see cref="KeypointTemplate"/>
        /// are assigned.
        /// </summary>
        public KeypointLabeler() {}

        /// <summary>
        /// Creates a new key point labeler.
        /// </summary>
        /// <param name="config">The Id label config for the labeler</param>
        /// <param name="template">The active keypoint template</param>
        public KeypointLabeler(IdLabelConfig config, KeypointTemplate template)
        {
            idLabelConfig = config;
            activeTemplate = template;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException($"{nameof(KeypointLabeler)}'s idLabelConfig field must be assigned");

            m_AnnotationDefinition = new KeypointAnnotationDefinition(
                annotationId, TemplateToJson(activeTemplate, idLabelConfig));
            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            visualizationEnabled = supportsVisualization;

            // Texture to use in case the template does not contain a texture for the joints or the skeletal connections
            m_MissingTexture = new Texture2D(1, 1);

            m_CurrentFrame = 0;

            m_DepthCheckMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/KeypointDepthCheck"));

            var depthChannel = perceptionCamera.EnableChannel<DepthChannel>();
            m_DepthTexture = depthChannel.outputTexture;

            var instanceIdChannel = perceptionCamera.EnableChannel<InstanceIdChannel>();
            instanceIdChannel.outputTextureReadback += OnInstanceSegmentationImageReadback;
            m_InstanceIdTexture = instanceIdChannel.outputTexture;
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfoReadback;
        }

        void SetupDepthCheckBuffers(int size)
        {
            var textureDimensions = TextureDimensions(size);
            if (m_ResultsBuffer != null &&
                textureDimensions.x == m_ResultsBuffer.width &&
                textureDimensions.y == m_ResultsBuffer.height)
                return;

            if (m_ResultsBuffer != null)
                m_ResultsBuffer.Release();

            m_KeypointPositionsTexture = new Texture2D(textureDimensions.x, textureDimensions.y,
                GraphicsFormat.R32G32_SFloat, TextureCreationFlags.None);
            m_KeypointCheckDepthTexture = new Texture2D(textureDimensions.x, textureDimensions.y,
                GraphicsFormat.R32_SFloat, TextureCreationFlags.None);
            m_ResultsBuffer = new RenderTexture(textureDimensions.x, textureDimensions.y,
                0, GraphicsFormat.R8G8B8A8_UNorm);
        }

        bool PixelOnScreen(int2 pixelLocation, (int x, int y) dimensions)
        {
            return pixelLocation.x >= 0 && pixelLocation.x < dimensions.x && pixelLocation.y >= 0 && pixelLocation.y < dimensions.y;
        }

        bool PixelsMatch(int x, int y, uint instanceId, (int x, int y) dimensions,
            NativeArray<uint> pixelInstanceIdIndices, NativeArray<uint> instanceIds)
        {
            var h = dimensions.y - 1 - y;
            var instanceIdIndex = pixelInstanceIdIndices[h * dimensions.x + x];
            var foundInstanceId = instanceIds[(int)instanceIdIndex];
            return foundInstanceId == instanceId;
        }

        // Determine the state of a keypoint. A keypoint is considered visible (state = 2) if it is on screen and not occluded
        // by itself or another object. Self-occlusion has already been checked, so the input keypoint may be state 2, 1, or 0.
        // We determine if a point is occluded by other objects is by checking the pixel location of the keypoint
        // against the instance segmentation mask for the frame. The instance segmentation mask provides the instance id of the
        // visible object at a pixel location. Which means, if the keypoint does not match the visible pixel, then another
        // object is in front of the keypoint occluding it from view. An important note here is that the keypoint is an infinitely small
        // point in space, which can lead to false negatives due to rounding issues if the keypoint is on the edge of an object or very
        // close to the edge of the screen. Because of this we will test not only the keypoint pixel, but also the immediate surrounding
        // pixels  to determine if the pixel is really visible. This method returns 1 if the pixel is not visible but on screen, and 0
        // if the pixel is off of the screen (taken the tolerance into account).
        int DetermineKeypointState(
            KeypointValue keypoint, uint instanceId, (int x, int y) dimensions,
            NativeArray<uint> pixelInstanceIdIndices, NativeArray<uint> cachedInstanceIds)
        {
            if (keypoint.state == 0) return 0;

            var pixelLocation = PixelLocationFromScreenPoint(keypoint);

            if (!PixelOnScreen(pixelLocation, dimensions))
                return 0;

            var pixelMatched = false;

            for (var y = pixelLocation.y - k_PixelTolerance; y <= pixelLocation.y + k_PixelTolerance; y++)
            {
                for (var x = pixelLocation.x - k_PixelTolerance; x <= pixelLocation.x + k_PixelTolerance; x++)
                {
                    if (!PixelOnScreen(new int2(x, y), dimensions)) continue;

                    pixelMatched = true;
                    if (PixelsMatch(x, y, instanceId, dimensions, pixelInstanceIdIndices, cachedInstanceIds))
                    {
                        return keypoint.state;
                    }
                }
            }

            return pixelMatched ? 1 : 0;
        }

        static int2 PixelLocationFromScreenPoint(KeypointValue keypoint)
        {
            var centerX = Mathf.FloorToInt(keypoint.location.x);
            var centerY = Mathf.FloorToInt(keypoint.location.y);
            var pixelLocation = new int2(centerX, centerY);
            return pixelLocation;
        }

        void OnInstanceSegmentationImageReadback(int frameCount, NativeArray<uint> pixelInstanceIdIndices)
        {
            if (!m_FrameKeypointData.TryGetValue(frameCount, out var frameKeypointData))
                return;

            var cachedInstanceIds = m_CachedInstanceIds[frameCount];
            m_CachedInstanceIds.Remove(frameCount);

            var dimensions = (m_InstanceIdTexture.width, m_InstanceIdTexture.height);

            foreach (var keypointEntry in frameKeypointData.keypoints)
            {
                if (keypointEntry.instanceId != 0)
                {
                    for (var i = 0; i < keypointEntry.keypoints.Length; i++)
                    {
                        var keypoint = keypointEntry.keypoints[i];
                        keypoint.state = DetermineKeypointState(
                            keypoint, keypointEntry.instanceId, dimensions, pixelInstanceIdIndices, cachedInstanceIds);

                        if (keypoint.state == 0)
                        {
                            keypoint.location = Vector2.zero;
                            keypoint.cameraCartesianLocation = Vector3.zero;
                        }
                        else
                        {
                            var location = keypoint.location;
                            location.x = math.clamp(keypoint.location.x, 0, dimensions.width - .001f);
                            location.y = math.clamp(keypoint.location.y, 0, dimensions.height - .001f);
                            keypoint.location = location;
                        }

                        keypointEntry.keypoints[i] = keypoint;
                    }
                }
            }

            cachedInstanceIds.Dispose();

            frameKeypointData.isInstanceSegmentationCheckComplete = true;
            m_FrameKeypointData[frameCount] = frameKeypointData;
            ReportIfComplete(frameCount, frameKeypointData);
        }

        void OnRenderedObjectInfoReadback(
            int frameCount, NativeArray<RenderedObjectInfo> objectInfos,
            SceneHierarchyInformation hierarchyInfo)
        {
            if (!m_FrameKeypointData.TryGetValue(frameCount, out var frameKeypointData))
                return;

            frameKeypointData.objectInfos = new NativeArray<RenderedObjectInfo>(objectInfos, Allocator.Persistent);
            m_FrameKeypointData[frameCount] = frameKeypointData;
            ReportIfComplete(frameCount, frameKeypointData);
        }

        void ReportIfComplete(int frameCount, FrameKeypointData frameKeypointData)
        {
            if (!frameKeypointData.isInstanceSegmentationCheckComplete || !frameKeypointData.isDepthCheckComplete || !frameKeypointData.objectInfos.IsCreated)
                return;

            var reportList = new List<KeypointComponent>();

            //filter out objects that are not visible
            foreach (var entry in frameKeypointData.keypoints)
            {
                var include = false;
                if (objectFilter == KeypointObjectFilter.All)
                    include = true;
                else
                {
                    foreach (var objectInfo in frameKeypointData.objectInfos)
                    {
                        if (entry.instanceId == objectInfo.instanceId)
                        {
                            include = true;
                            break;
                        }
                    }

                    if (!include && objectFilter == KeypointObjectFilter.VisibleAndOccluded)
                        include = entry.keypoints.Any(k => k.state == 1);
                }

                if (include)
                    reportList.Add(entry);
            }
            m_FrameKeypointData.Remove(frameCount);
            KeypointsComputed?.Invoke(frameCount, reportList);
            var toReport = new KeypointAnnotation(m_AnnotationDefinition, perceptionCamera.id, activeTemplate.templateID, reportList);
            frameKeypointData.annotation.Report(toReport);
            frameKeypointData.objectInfos.Dispose();
            m_LatestReported = reportList;
        }

        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            using (s_OnEndRenderingMarker.Auto())
            {
                m_CurrentFrame = Time.frameCount;

                var annotation = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
                var keypointEntries = new List<KeypointComponent>();
                var checkLocations = new NativeList<float3>(512, Allocator.Persistent);

                foreach (var label in LabelManager.singleton.registeredLabels)
                    ProcessLabel(label, keypointEntries, checkLocations);

                m_FrameKeypointData[m_CurrentFrame] = new FrameKeypointData
                {
                    annotation = annotation,
                    keypoints = keypointEntries,
                    pointsPerEntry = activeTemplate.keypoints.Length
                };

                m_CachedInstanceIds[m_CurrentFrame] = new NativeArray<uint>(
                    LabelManager.singleton.instanceIds, Allocator.Persistent);

                if (keypointEntries.Count != 0)
                    DoDepthCheck(scriptableRenderContext, keypointEntries, checkLocations);
                else
                {
                    var frameKeypointData = m_FrameKeypointData[m_CurrentFrame];
                    frameKeypointData.isDepthCheckComplete = true;
                    m_FrameKeypointData[m_CurrentFrame] = frameKeypointData;
                }

                checkLocations.Dispose();
            }
        }

        /// Check self occlusion of each keypoint by passing keypoint location (x & y in one texture) and modified distance from camera (keypoint distance - keypoint threshold distance)
        /// in an additional texture. The computer shader checks the depth buffer at each passed in location, converts the depth at the pixel to linear space, and then compares it to
        /// the passed in modified keypoint distance. If the modified keypoint distance is less than the depth buffer distance, the keypoint is visible, else it is blocked by itself.
        void DoDepthCheck(ScriptableRenderContext scriptableRenderContext, List<KeypointComponent> keypointEntries, NativeList<float3> checkLocations)
        {
            var keypointCount = keypointEntries.Count * activeTemplate.keypoints.Length;

            var cmd = CommandBufferPool.Get("KeypointDepthCheck");

            var textureDimensions = TextureDimensions(keypointCount);

            SetupDepthCheckBuffers(checkLocations.Length);

            var positionsPixelData = new NativeArray<float2>(
                textureDimensions.x * textureDimensions.y, Allocator.Temp);
            var depthPixelData = new NativeArray<float>(
                textureDimensions.x * textureDimensions.y, Allocator.Temp);

            for (var i = 0; i < checkLocations.Length; i++)
            {
                var pos = checkLocations[i];
                positionsPixelData[i] = new float2(pos.x, pos.y);
                depthPixelData[i] = pos.z;
            }

            m_KeypointPositionsTexture.SetPixelData(positionsPixelData, 0);
            m_KeypointPositionsTexture.Apply();
            m_KeypointCheckDepthTexture.SetPixelData(depthPixelData, 0);
            m_KeypointCheckDepthTexture.Apply();

            positionsPixelData.Dispose();
            depthPixelData.Dispose();

            cmd.SetGlobalTexture(k_KeypointPositions, m_KeypointPositionsTexture);
            cmd.SetGlobalTexture(k_KeypointDepthToCheck, m_KeypointCheckDepthTexture);
            cmd.SetGlobalTexture(k_LinearDepthTexture, m_DepthTexture);
            cmd.SetGlobalInteger(k_CameraPixelHeight, perceptionCamera.cameraSensor.pixelHeight);
            cmd.SetGlobalFloat(k_CameraFarPlane, perceptionCamera.attachedCamera.farClipPlane);

            cmd.SetRenderTarget(m_ResultsBuffer);
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.Blit(null, m_ResultsBuffer, m_DepthCheckMaterial);

            RenderTextureReader.Capture<Color32>(cmd, m_ResultsBuffer,
                (frame, data, _) => DoDepthCheckReadback(frame, data));
            cmd.SetRenderTarget((RenderTexture)null);

            scriptableRenderContext.ExecuteCommandBuffer(cmd);
            scriptableRenderContext.Submit();
            CommandBufferPool.Release(cmd);
        }

        static Vector2Int TextureDimensions(int keypointCount)
        {
            var width = Math.Max(k_MinTextureWidth, Mathf.NextPowerOfTwo((int)Math.Ceiling(Math.Sqrt(keypointCount))));
            var height = width;

            var textureDimensions = new Vector2Int(width, height);
            return textureDimensions;
        }

        /// <summary>
        /// Iterate through each keypoint to check if the depth check shader has determined if it is visible.
        /// A keypoint is visible if its results texture pixel has an red channel value of 1.
        /// </summary>
        void DoDepthCheckReadback(int frameCount, NativeArray<Color32> data)
        {
            var frameKeypointData = m_FrameKeypointData[frameCount];
            var totalLength = frameKeypointData.pointsPerEntry * frameKeypointData.keypoints.Count;
            Debug.Assert(totalLength < data.Length);
            for (var i = 0; i < totalLength; i++)
            {
                var depthCheckResult = data[i];
                if (depthCheckResult.r == 0)
                {
                    var keypoints = frameKeypointData.keypoints[i / frameKeypointData.pointsPerEntry];
                    var indexInObject = i % frameKeypointData.pointsPerEntry;
                    var keypoint = keypoints.keypoints[indexInObject];
                    keypoint.state = 1;
                    keypoints.keypoints[indexInObject] = keypoint;
                }
            }

            frameKeypointData.isDepthCheckComplete = true;
            m_FrameKeypointData[frameCount] = frameKeypointData;
            ReportIfComplete(frameCount, frameKeypointData);
        }

        float GetCaptureHeight()
        {
            var targetTexture = perceptionCamera.attachedCamera.targetTexture;
            return targetTexture != null ?
                targetTexture.height : Screen.height;
        }

        Vector3 ConvertToScreenSpace(Vector3 worldLocation)
        {
            var pt = perceptionCamera.attachedCamera.WorldToScreenPoint(worldLocation);
            pt.y = GetCaptureHeight() - pt.y;
            if (Mathf.Approximately(pt.y, perceptionCamera.attachedCamera.pixelHeight))
                pt.y -= .0001f;
            if (Mathf.Approximately(pt.x, perceptionCamera.attachedCamera.pixelWidth))
                pt.x -= .0001f;

            return pt;
        }

        Vector3 ConvertToCameraSpace(Vector3 worldLocation)
        {
            return perceptionCamera.attachedCamera.transform.InverseTransformPoint(worldLocation);
        }

        bool TryToGetTemplateIndexForJoint(KeypointTemplate template, JointLabel joint, out int index)
        {
            index = -1;

            foreach (var label in joint.labels)
            {
                for (var i = 0; i < template.keypoints.Length; i++)
                {
                    if (template.keypoints[i].label == label)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            return false;
        }

        bool DoesTemplateContainJoint(JointLabel jointLabel)
        {
            return TryToGetTemplateIndexForJoint(activeTemplate, jointLabel, out _);
        }

        void ProcessLabel(Labeling labeledEntity, List<KeypointComponent> keypointEntries, NativeList<float3> checkLocations)
        {
            if (!idLabelConfig.TryGetLabelEntryFromInstanceId(labeledEntity.instanceId, out var labelEntry))
                return;

            var keypointsFound = false;

            var overrides = new List<(JointLabel, int)>();

            var entityGameObject = labeledEntity.gameObject;

            var animator = entityGameObject.transform.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                keypointsFound = true;
            }

            foreach (var joint in entityGameObject.transform.GetComponentsInChildren<JointLabel>())
            {
                if (TryToGetTemplateIndexForJoint(activeTemplate, joint, out var idx))
                {
                    overrides.Add((joint, idx));
                    keypointsFound = true;
                }
            }

            if (keypointsFound)
            {
                var keypointComponent = new KeypointComponent(labelEntry.id, labeledEntity.instanceId, "unset", activeTemplate.keypoints.Length);

                var occlusionScalar = 1.0f;

                var occlusionOverrider = labeledEntity.GetComponentInParent<KeypointOcclusionOverrides>();
                if (occlusionOverrider != null)
                {
                    occlusionScalar = occlusionOverrider.distanceScale;
                }

                var listStart = checkLocations.Length;
                checkLocations.Resize(checkLocations.Length + activeTemplate.keypoints.Length, NativeArrayOptions.ClearMemory);
                //grab the slice of the list for the current object to assign positions in
                var checkLocationsSlice = new NativeSlice<float3>(checkLocations, listStart);

                var transform = perceptionCamera.transform;
                var cameraPosition = transform.position;
                var cameraForward = transform.forward;

                if (animator != null && animator.gameObject.activeSelf)
                {
                    // Go through all of the rig keypoints and get their location
                    for (var i = 0; i < activeTemplate.keypoints.Length; i++)
                    {
                        var pt = activeTemplate.keypoints[i];
                        if (pt.associateToRig)
                        {
                            var bone = animator.GetBoneTransform(pt.rigLabel);
                            if (bone != null)
                            {
                                var bonePosition = bone.position;

                                var occlusionDistance = pt.selfOcclusionDistance * occlusionScalar;
                                var jointSelfOcclusionDistance = JointSelfOcclusionDistance(bone, bonePosition, cameraPosition, cameraForward, occlusionDistance);

                                InitKeypoint(bonePosition, keypointComponent, checkLocationsSlice, i, jointSelfOcclusionDistance);
                            }
                        }
                    }
                }

                // Go through all of the additional or override points defined by joint labels and get
                // their locations
                foreach (var(joint, templateIdx) in overrides)
                {
                    var jointTransform = joint.transform;
                    var jointPosition = jointTransform.position;
                    float resolvedSelfOcclusionDistance;
                    if (joint.overrideSelfOcclusionDistance)
                        resolvedSelfOcclusionDistance = joint.selfOcclusionDistance;
                    else
                        resolvedSelfOcclusionDistance = activeTemplate.keypoints[templateIdx].selfOcclusionDistance;

                    resolvedSelfOcclusionDistance *= occlusionScalar;

                    var jointSelfOcclusionDistance = JointSelfOcclusionDistance(joint.transform, jointPosition, cameraPosition, cameraForward, resolvedSelfOcclusionDistance);

                    InitKeypoint(jointPosition, keypointComponent, checkLocationsSlice, templateIdx, jointSelfOcclusionDistance);
                }

                keypointComponent.pose = "unset";

                if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                {
                    keypointComponent.pose = GetPose(animator);
                }

                keypointEntries.Add(keypointComponent);
            }
        }

        KeypointValue[] DeepCopyKeypoints(IReadOnlyList<KeypointValue> values)
        {
            var copied = new KeypointValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                copied[i] = values[i].Clone() as KeypointValue;
            }

            return copied;
        }

        float JointSelfOcclusionDistance(Transform transform, Vector3 jointPosition, Vector3 cameraPosition,
            Vector3 cameraforward, float configuredSelfOcclusionDistance)
        {
            var depthOfJoint = Vector3.Dot(jointPosition - cameraPosition, cameraforward);
            var cameraEffectivePosition = jointPosition - cameraforward * depthOfJoint;

            var jointRelativeCameraPosition = transform.InverseTransformPoint(cameraEffectivePosition);
            var jointRelativeCheckPosition = jointRelativeCameraPosition.normalized * configuredSelfOcclusionDistance;
            var worldSpaceCheckVector = transform.TransformVector(jointRelativeCheckPosition);
            return worldSpaceCheckVector.magnitude;
        }

        void InitKeypoint(Vector3 position, KeypointComponent keypointComponent, NativeSlice<float3> checkLocations, int idx,
            float occlusionDistance)
        {
            var loc = ConvertToScreenSpace(position);
            var cameraLoc = ConvertToCameraSpace(position);

            var keypoints = keypointComponent.keypoints;
            keypoints[idx].index = idx;
            if (loc.z < 0)
            {
                keypoints[idx].location = Vector2.zero;
                keypoints[idx].cameraCartesianLocation = Vector3.zero;
                keypoints[idx].state = 0;
            }
            else
            {
                keypoints[idx].location = new Vector2(loc.x, loc.y);
                keypoints[idx].cameraCartesianLocation = cameraLoc;
                keypoints[idx].state = 2;
            }

            //TODO: move this code
            var pixelLocation = PixelLocationFromScreenPoint(keypoints[idx]);
            if (pixelLocation.x < 0 || pixelLocation.y < 0)
            {
                pixelLocation = new int2(int.MaxValue, int.MaxValue);
            }

            checkLocations[idx] = new float3(pixelLocation.x + .5f, pixelLocation.y + .5f, loc.z - occlusionDistance);
        }

        string GetPose(Animator animator)
        {
            if (animator == null || !animator.isActiveAndEnabled || !animator.gameObject.activeSelf)
            {
                return string.Empty;
            }

            var info = animator.GetCurrentAnimatorClipInfo(0);

            if (info != null && info.Length > 0)
            {
                var clip = info[0].clip;
                var timeOffset = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                if (animationPoseConfigs != null)
                {
                    foreach (var p in animationPoseConfigs)
                    {
                        if (p != null && p.animationClip == clip)
                        {
                            var time = timeOffset;
                            var label = p.GetPoseAtTime(time);
                            return label;
                        }
                    }
                }
            }

            return "unset";
        }

        KeypointValue GetKeypointForJoint(KeypointComponent entry, int joint)
        {
            if (joint < 0 || joint >= entry.keypoints.Length) return null;
            return entry.keypoints[joint];
        }

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            if (m_LatestReported == null) return;
            using (s_OnVisualizeMarker.Auto())
            {
                var jointTexture = activeTemplate.jointTexture;
                if (jointTexture == null) jointTexture = m_MissingTexture;

                var skeletonTexture = activeTemplate.skeletonTexture;
                if (skeletonTexture == null) skeletonTexture = m_MissingTexture;

                var occludedJointTexture = activeTemplate.occludedJointTexture;
                if (occludedJointTexture == null) occludedJointTexture = m_MissingTexture;

                foreach (var entry in m_LatestReported)
                {
                    foreach (var bone in activeTemplate.skeleton)
                    {
                        var joint1 = GetKeypointForJoint(entry, bone.joint1);
                        var joint2 = GetKeypointForJoint(entry, bone.joint2);

                        if (joint1 != null && joint1.state == 2 && joint2 != null && joint2.state == 2)
                        {
                            VisualizationHelper.DrawLine(joint1.location.x, joint1.location.y, joint2.location.x, joint2.location.y, bone.color, 8, skeletonTexture);
                        }
                    }

                    foreach (var keypoint in entry.keypoints)
                    {
                        if (keypoint.state == 2)
                            VisualizationHelper.DrawPoint(keypoint.location.x, keypoint.location.y, activeTemplate.keypoints[keypoint.index].color, 6, jointTexture);
                        else if (visualizeOccludedPoints && keypoint.state == 1)
                            VisualizationHelper.DrawPoint(keypoint.location.x, keypoint.location.y, activeTemplate.occludedJointColor, 6, occludedJointTexture);
                    }
                }
            }
        }

        // TODO rename this method
        KeypointAnnotationDefinition.Template TemplateToJson(KeypointTemplate input, IdLabelConfig labelConfig)
        {
            var json = new KeypointAnnotationDefinition.Template
            {
                templateId = input.templateID,
                templateName = input.templateName,
                keyPoints = new KeypointAnnotationDefinition.JointDefinition[input.keypoints.Length],
                skeleton = new KeypointAnnotationDefinition.SkeletonDefinition[input.skeleton.Length]
            };

            for (var i = 0; i < input.keypoints.Length; i++)
            {
                json.keyPoints[i] = new KeypointAnnotationDefinition.JointDefinition
                {
                    label = input.keypoints[i].label,
                    index = i,
                    color = input.keypoints[i].color
                };
            }

            for (var i = 0; i < input.skeleton.Length; i++)
            {
                json.skeleton[i] = new KeypointAnnotationDefinition.SkeletonDefinition
                {
                    joint1 = input.skeleton[i].joint1,
                    joint2 = input.skeleton[i].joint2,
                    color = input.skeleton[i].color
                };
            }

            return json;
        }
    }
}
