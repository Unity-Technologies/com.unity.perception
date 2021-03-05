using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Serialization;
using Unity.Simulation;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces 2d bounding box annotations for all visible objects each frame.
    /// </summary>
    [Serializable]
    public sealed class BoundingBox2DLabeler : CameraLabeler
    {
        ///<inheritdoc/>
        public override string description
        {
            get => "Produces 2D bounding box annotations for all visible objects that bear a label defined in this labeler's associated label configuration.";
            protected set {}
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        struct BoundingBoxValue
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
            public float x;
            public float y;
            public float width;
            public float height;
        }

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxesReceived");

        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "f9f22e05-443f-4602-a422-ebe4ea9b55cb";
        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;
        AnnotationDefinition m_BoundingBoxAnnotationDefinition;
        List<BoundingBoxValue> m_BoundingBoxValues;

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

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox2DLabeler's idLabelConfig field must be assigned");

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();
            m_BoundingBoxValues = new List<BoundingBoxValue>();

            m_BoundingBoxAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box", idLabelConfig.GetAnnotationSpecification(),
                "Bounding box for each labeled object visible to the sensor", id: new Guid(annotationId));

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

        /// <param name="scriptableRenderContext"></param>
        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_BoundingBoxAnnotationDefinition);
        }

        void OnRenderedObjectInfosCalculated(int frameCount, NativeArray<RenderedObjectInfo> renderedObjectInfos)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var asyncAnnotation))
                return;

            m_AsyncAnnotations.Remove(frameCount);
            using (s_BoundingBoxCallback.Auto())
            {
                m_BoundingBoxValues.Clear();
                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!idLabelConfig.TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_BoundingBoxValues.Add(new BoundingBoxValue
                    {
                        label_id = labelEntry.id,
                        label_name = labelEntry.label,
                        instance_id = objectInfo.instanceId,
                        x = objectInfo.boundingBox.x,
                        y = objectInfo.boundingBox.y,
                        width = objectInfo.boundingBox.width,
                        height = objectInfo.boundingBox.height,
                    });
                }

                if (!CaptureOptions.useAsyncReadbackIfSupported && frameCount != Time.frameCount)
                    Debug.LogWarning("Not on current frame: " + frameCount + "(" + Time.frameCount + ")");

                asyncAnnotation.ReportValues(m_BoundingBoxValues);
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            if (m_BoundingBoxValues == null) return;

            GUI.depth = 5;

            // The player screen can be dynamically resized during play, need to
            // scale the bounding boxes appropriately from the original screen size
            var screenRatioWidth = Screen.width / m_OriginalScreenSize.x;
            var screenRatioHeight = Screen.height / m_OriginalScreenSize.y;

            foreach (var box in m_BoundingBoxValues)
            {
                var x = box.x * screenRatioWidth;
                var y = box.y * screenRatioHeight;

                var boxRect = new Rect(x, y, box.width * screenRatioWidth, box.height * screenRatioHeight);
                var labelWidth = Math.Min(120, box.width * screenRatioWidth);
                var labelRect = new Rect(x, y - 17, labelWidth, 17);
                GUI.DrawTexture(boxRect, m_BoundingBoxTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 3, 0.25f);
                GUI.DrawTexture(labelRect, m_LabelTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 0, 0);
                GUI.Label(labelRect, box.label_name + "_" + box.instance_id, m_Style);
            }
        }
    }
}
