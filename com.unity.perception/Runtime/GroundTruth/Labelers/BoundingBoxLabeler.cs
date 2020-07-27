using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Serialization;
using Unity.Simulation;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces 2d bounding box annotations for all visible objects each frame.
    /// </summary>
    [Serializable]
    public sealed class BoundingBox2DLabeler : CameraLabeler
    {
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
        public string annotationId = "F9F22E05-443F-4602-A422-EBE4EA9B55CB";
        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;
        AnnotationDefinition m_BoundingBoxAnnotationDefinition;
        BoundingBoxValue[] m_BoundingBoxValues;

        private GameObject visualizationHolder = null;

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

            m_BoundingBoxAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box", idLabelConfig.GetAnnotationSpecification(),
                "Bounding box for each labeled object visible to the sensor", id: new Guid(annotationId));

            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfosCalculated;

            visualizationEnabled = supportsVisualization;
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering()
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
                if (m_BoundingBoxValues == null || m_BoundingBoxValues.Length != renderedObjectInfos.Length)
                    m_BoundingBoxValues = new BoundingBoxValue[renderedObjectInfos.Length];

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!idLabelConfig.TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_BoundingBoxValues[i] = new BoundingBoxValue
                    {
                        label_id = labelEntry.id,
                        label_name = labelEntry.label,
                        instance_id = objectInfo.instanceId,
                        x = objectInfo.boundingBox.x,
                        y = objectInfo.boundingBox.y,
                        width = objectInfo.boundingBox.width,
                        height = objectInfo.boundingBox.height,
                    };
                }

                if (!CaptureOptions.useAsyncReadbackIfSupported && frameCount != Time.frameCount) 
                    Debug.LogWarning("Not on current frame: " + frameCount + "(" + Time.frameCount + ")");

                if (perceptionCamera.visualizationEnabled && visualizationEnabled)
                {
                    Visualize();
                }
                
                asyncAnnotation.ReportValues(m_BoundingBoxValues);
            }
        }

        /// <inheritdoc/>
        protected override void PopulateVisualizationPanel(ControlPanel panel)
        {
            panel.AddToggleControl("BoundingBoxes", enabled => {
                visualizationEnabled = enabled;
            });

            objectPool = new List<GameObject>();

            visualizationHolder = new GameObject("BoundsHolder" + Time.frameCount);
            canvas.AddComponent(visualizationHolder);
        }

        void ClearObjectPool(int count)
        {
            for (int i = count; i < objectPool.Count; i++)
            {
                objectPool[i].SetActive(false);
            }
        }

        List<GameObject> objectPool = null;

        void Visualize()
        {
            ClearObjectPool(m_BoundingBoxValues.Length);

            for (int i = 0; i < m_BoundingBoxValues.Length; i++)
            {
                var boxVal = m_BoundingBoxValues[i];

                if (i >= objectPool.Count)
                {
                    objectPool.Add(GameObject.Instantiate(Resources.Load<GameObject>("BoundingBoxPrefab")));
                    (objectPool[i].transform as RectTransform).parent = visualizationHolder.transform;
                }

                if (!objectPool[i].activeSelf) objectPool[i].SetActive(true);

                string label = boxVal.label_name + "_" + boxVal.instance_id;
                objectPool[i].GetComponentInChildren<Text>().text = label;

                var rectTrans = objectPool[i].transform as RectTransform;
                
                rectTrans.anchoredPosition = new Vector2(boxVal.x, -boxVal.y);
                rectTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boxVal.width);
                rectTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boxVal.height);
            }
        }

        /// <inheritdoc/>
        override protected void OnVisualizerActiveStateChanged(bool enabled)
        {
            if (visualizationHolder != null) 
                visualizationHolder.SetActive(enabled);
        }
    }
}
