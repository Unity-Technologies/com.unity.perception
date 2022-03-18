using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces 2d bounding box annotations for all visible objects each frame.
    /// </summary>
    [Serializable]
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
            /// The <see cref="Time.frameCount"/> on which the data was derived. This may be multiple frames in the past.
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

        void OnRenderedObjectInfosCalculated(int frameCount, NativeArray<RenderedObjectInfo> renderedObjectInfos)
        {
            if (!m_AsyncData.TryGetValue(frameCount, out var asyncData))
                return;

            m_AsyncData.Remove(frameCount);
            using (s_BoundingBoxCallback.Auto())
            {
                var bbValues = new List<BoundingBox>();
                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!asyncData.labelEntryMatchCache.TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry, out _))
                        continue;

                    bbValues.Add(new BoundingBox
                        {
                            labelId = labelEntry.id,
                            labelName = labelEntry.label,
                            instanceId = (int)objectInfo.instanceId,
                            origin = new Vector2(objectInfo.boundingBox.x, objectInfo.boundingBox.y),
                            dimension = new Vector2(objectInfo.boundingBox.width, objectInfo.boundingBox.height)
                        }
                    );
                }

                if (!PerceptionCamera.useAsyncReadbackIfSupported && frameCount != Time.frameCount)
                    Debug.LogWarning("Not on current frame: " + frameCount + "(" + Time.frameCount + ")");

                m_ToVisualize = bbValues;

                boundingBoxesCalculated?.Invoke(new BoundingBoxesCalculatedEventArgs()
                {
                    data = bbValues,
                    frameCount = frameCount
                });

                var toReport = new BoundingBoxAnnotation(m_AnnotationDefinition, perceptionCamera.ID, bbValues);
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

                var boxRect = new Rect(x, y, box.dimension.x * screenRatioWidth, box.dimension.y * screenRatioHeight);
                var labelWidth = Math.Min(120, box.dimension.x * screenRatioWidth);
                var labelRect = new Rect(x, y - 17, labelWidth, 17);
                GUI.DrawTexture(boxRect, m_BoundingBoxTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 3, 0.25f);
                GUI.DrawTexture(labelRect, m_LabelTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 0, 0);
                GUI.Label(labelRect, box.labelName + "_" + box.instanceId, m_Style);
            }
        }
    }
}
