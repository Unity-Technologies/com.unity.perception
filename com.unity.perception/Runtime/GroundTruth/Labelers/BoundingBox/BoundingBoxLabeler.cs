using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Produces 2d bounding box annotations for all visible objects each frame.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class BoundingBox2DLabeler : CameraLabeler
    {
        BoundingBoxDefinition m_AnnotationDefinition;

        ///<inheritdoc/>
        public override string description => BoundingBoxDefinition.labelerDescription;

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxesReceived");

        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "bounding box";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        Dictionary<int, (AsyncFuture<DataModel.Annotation> annotation, LabelEntryMatchCache labelEntryMatchCache)> m_AsyncData;
        List<BoundingBox> m_ToVisualize;

        Vector2 m_OriginalScreenSize = Vector2.zero;

        Texture m_BoundingBoxTexture;
        Texture m_LabelTexture;
        GUIStyle m_Style;

        /// <summary>
        /// Creates a new BoundingBox2DLabeler. Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public BoundingBox2DLabeler()
        {
        }

        /// <summary>
        /// Creates a new BoundingBox2DLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config for resolving the label for each object.</param>
        public BoundingBox2DLabeler(IdLabelConfig labelConfig)
        {
            this.idLabelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <summary>
        /// Event information for <see cref="BoundingBox2DLabeler.boundingBoxesCalculated"/>
        /// </summary>
        internal struct BoundingBoxesCalculatedEventArgs
        {
            /// <summary>
            /// The <see cref="SimulationState.CurrentFrameIndex"/> on which the data was derived. This may be multiple frames in the past.
            /// </summary>
            public int frameCount;
            /// <summary>
            /// Bounding boxes.
            /// </summary>
            public IEnumerable<BoundingBox> data;
        }

        /// <summary>
        /// Event which is called each frame a semantic segmentation image is read back from the GPU.
        /// </summary>
        internal event Action<BoundingBoxesCalculatedEventArgs> boundingBoxesCalculated;

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox2DLabeler's idLabelConfig field must be assigned");

            m_AsyncData = new Dictionary<int, (AsyncFuture<DataModel.Annotation> annotation, LabelEntryMatchCache labelEntryMatchCache)>();

            m_AnnotationDefinition = new BoundingBoxDefinition(annotationId, idLabelConfig.GetAnnotationSpecification());

            DatasetCapture.RegisterAnnotationDefinition(m_AnnotationDefinition);

            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfosCalculated;

            visualizationEnabled = supportsVisualization;

            // Record the original screen size. The screen size can change during play, and the visual bounding
            // boxes need to be scaled appropriately
            m_OriginalScreenSize = new Vector2(Screen.width, Screen.height);

            m_BoundingBoxTexture = Resources.Load<Texture>("outline_box");
            m_LabelTexture = Resources.Load<Texture>("solid_white");

            m_Style = new GUIStyle();
            m_Style.normal.textColor = Color.black;
            m_Style.fontSize = 16;
            m_Style.padding = new RectOffset(4, 4, 4, 4);
            m_Style.contentOffset = new Vector2(4, 0);
            m_Style.alignment = TextAnchor.MiddleLeft;
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_AsyncData[Time.frameCount] =
                (perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition),
                    idLabelConfig.CreateLabelEntryMatchCache(Allocator.TempJob));
        }

        class IntermediaryBoundingBoxData
        {
            public BoundingBox boundingBox;
            public IdLabelEntry? labelEntry;
            public bool IsInLabelConfig => labelEntry.HasValue;
            public bool hasEncapsulatedAlready = false;
        }

        void OnRenderedObjectInfosCalculated(
            int frameCount,
            NativeArray<RenderedObjectInfo> renderedObjectInfos,
            SceneHierarchyInformation hierarchyInfo
        )
        {
            if (!m_AsyncData.TryGetValue(frameCount, out var asyncData))
                return;

            m_AsyncData.Remove(frameCount);
            using (s_BoundingBoxCallback.Auto())
            {
                var boxes = new Dictionary<uint, IntermediaryBoundingBoxData>();
                // go through the array once and make the individual separate bounding boxes
                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    var instanceId = objectInfo.instanceId;

                    var im = new IntermediaryBoundingBoxData()
                    {
                        boundingBox = new BoundingBox
                        {
                            instanceId = (int)instanceId,
                            origin = new Vector2(objectInfo.boundingBox.x, objectInfo.boundingBox.y),
                            dimension = new Vector2(objectInfo.boundingBox.width, objectInfo.boundingBox.height)
                        }
                    };

                    // if the current bounding box is in the selected label config,
                    // add some extra labelEntry-related information
                    if (asyncData.labelEntryMatchCache.TryGetLabelEntryFromInstanceId(instanceId, out var labelEntry, out _))
                    {
                        im.boundingBox.labelId = labelEntry.id;
                        im.boundingBox.labelName = labelEntry.label;
                        im.labelEntry = labelEntry;
                    }

                    boxes[instanceId] = im;
                }

                // Combination Process
                // for each bounding box, if it wants to be merged into parent, we:
                //   1. ensure its the largest it can be (recursively make it as big as its children)
                //   2. enlarge its parent bounding box with its bounding box
                void EnlargeIfNeeded(uint nodeInstanceId)
                {
                    // get bounding box and hierarchy of current node
                    var nodeIB = boxes[nodeInstanceId];
                    if (nodeIB.hasEncapsulatedAlready)
                        return;

                    var hierarchyNode = hierarchyInfo.hierarchy[nodeInstanceId];
                    // for each child, check if the our bounding box needs to expand to fit it
                    foreach (var childInstanceId in hierarchyNode.childrenInstanceIds)
                    {
                        // recursively make sure that the child bounding box is big enough!
                        EnlargeIfNeeded(childInstanceId);

                        // invariant: child is as big as it should be
                        var childNodeIb = boxes[childInstanceId];
                        if (
                            !childNodeIb.IsInLabelConfig ||
                            childNodeIb.labelEntry is { hierarchyRelation: HierarchyRelation.AddToParent }
                        )
                        {
                            nodeIB.boundingBox.Encapsulate(childNodeIb.boundingBox);
                        }
                    }

                    nodeIB.hasEncapsulatedAlready = true;
                    boxes[nodeInstanceId] = nodeIB;
                }

                // go through all instance ids again for the "combination" process
                foreach (var instanceId in renderedObjectInfos)
                    EnlargeIfNeeded(instanceId.instanceId);

                var finalBoxes = new List<BoundingBox>();
                foreach (var(instanceId, im) in boxes)
                {
                    if (im.IsInLabelConfig)
                        finalBoxes.Add(im.boundingBox);
                }

                m_ToVisualize = finalBoxes;
                boundingBoxesCalculated?.Invoke(new BoundingBoxesCalculatedEventArgs() {
                    data = finalBoxes,
                    frameCount = frameCount
                });

                var toReport = new BoundingBoxAnnotation(m_AnnotationDefinition, perceptionCamera.id, finalBoxes);
                asyncData.annotation.Report(toReport);
                asyncData.labelEntryMatchCache.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            if (m_ToVisualize == null) return;

            GUI.depth = 5;

            // The player screen can be dynamically resized during play, need to
            // scale the bounding boxes appropriately from the original screen size
            var screenRatioWidth = Screen.width / m_OriginalScreenSize.x;
            var screenRatioHeight = Screen.height / m_OriginalScreenSize.y;

            foreach (var box in m_ToVisualize)
            {
                var x = box.origin.x * screenRatioWidth;
                var y = box.origin.y * screenRatioHeight;
                InstanceIdToColorMapping.TryGetColorFromInstanceId((uint)box.instanceId, out var color);

                var boxRect = new Rect(x, y, box.dimension.x * screenRatioWidth, box.dimension.y * screenRatioHeight);
                var labelWidth = Math.Min(120, box.dimension.x * screenRatioWidth);
                var labelRect = new Rect(x, y - 17, labelWidth, 17);
                GUI.DrawTexture(boxRect, m_BoundingBoxTexture, ScaleMode.StretchToFill, true, 0, color, 3, 0.25f);
                GUI.DrawTexture(labelRect, m_LabelTexture, ScaleMode.StretchToFill, true, 0, color, 0, 0);
                GUI.Label(labelRect, box.labelName + "_" + box.instanceId, m_Style);
            }
        }
    }
}
