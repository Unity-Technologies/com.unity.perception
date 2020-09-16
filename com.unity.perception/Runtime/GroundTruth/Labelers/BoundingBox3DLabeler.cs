using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Entities;
using Unity.Profiling;
using Unity.Simulation;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.GroundTruth
{
    // ##### 3D bounding box
    //
    // A json file that stored collections of 3D bounding boxes.
    // Each bounding box record maps a tuple of (instance, label) to translation, size and rotation that draws a 3D bounding box,
    // as well as velocity and acceleration (optional) of the 3D bounding box.
    // All location data is given with respect to the **sensor coordinate system**.
    //
    //
    // bounding_box_3d {
    //      label_id:     <int> -- Integer identifier of the label
    //      label_name:   <str> -- String identifier of the label
    //      instance_id:  <str> -- UUID of the instance.
    //      translation:  <float, float, float> -- 3d bounding box's center location in meters as center_x, center_y, center_z with respect to global coordinate system.
    //      size:         <float, float, float> -- 3d bounding box size in meters as width, length, height.
    //      rotation:     <float, float, float, float> -- 3d bounding box orientation as quaternion: w, x, y, z.
    //      velocity:     <float, float, float>  -- 3d bounding box velocity in meters per second as v_x, v_y, v_z.
    //      acceleration: <float, float, float> [optional] -- 3d bounding box acceleration in meters per second^2 as a_x, a_y, a_z.
    //  }

    public class BoundingBox3DLabeler : CameraLabeler, IGroundTruthUpdater
    {
        public override string description
        {
            get => "Produces 3D bounding box ground truth data for all visible objects that bear a label defined in this labeler's associated label configuration.";
            protected set {}
        }

        public enum OutputMode
        {
            Verbose,
            Kitti
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public abstract class BoxData
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
        }

        public class KittiData : BoxData
        {
            public float[] translation;
            public float[] size;
            public float yaw;
        }

        public class VerboseData : BoxData
        {
            public float[] translation;
            public float[] size;
            public float[] rotation;
            public float[] velocity; // TODO
            public float[] acceleration; // TODO
        }

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxes3DReceived");

        public string annotationId = "0bfbe00d-00fa-4555-88d1-471b58449f5c";

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;
        AnnotationDefinition m_AnnotationDefinition;
        BoxData[] m_BoundingBoxValues;

        public OutputMode mode = OutputMode.Kitti;

        public IdLabelConfig idLabelConfig;

        protected override bool supportsVisualization => false;

        public event Action<int, BoxData> BoundingBoxComputed;

        public BoundingBox3DLabeler() {}

        public BoundingBox3DLabeler(IdLabelConfig labelConfig)
        {
            this.idLabelConfig = labelConfig;
        }

        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox2DLabeler's idLabelConfig field must be assigned");

            var updater = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystem<GroundTruthUpdateSystem>();
            updater?.Activate(this);

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            m_AnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box 3D", idLabelConfig.GetAnnotationSpecification(),
                "Bounding box for each labeled object visible to the sensor", id: new Guid(annotationId));
        }

        protected override void Cleanup()
        {
            var updater = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthUpdateSystem>();
            updater?.Deactivate(this);
        }

        int m_CurrentIndex;
        int m_CurrentFrame;

        public void OnBeginUpdate(int count)
        {
            if (m_BoundingBoxValues == null || count != m_BoundingBoxValues.Length)
                m_BoundingBoxValues = new BoxData[count];

            m_CurrentIndex = 0;
            m_CurrentFrame = Time.frameCount;
        }

        static BoxData Convert(IdLabelEntry label, uint instanceId, Vector3 center, Vector3 extents, Quaternion rotation, OutputMode outputMode)
        {
            return outputMode == OutputMode.Kitti ?
                ConvertToKitti(label, instanceId, center, extents, rotation) :
                ConvertToVerboseData(label, instanceId, center, extents, rotation);
        }

        static BoxData ConvertToVerboseData(IdLabelEntry label, uint instanceId, Vector3 center, Vector3 extents, Quaternion rotation)
        {
            return new VerboseData
            {
                label_id = label.id,
                label_name = label.label,
                instance_id = instanceId,
                translation = new float[] { center.x, center.y, center.z },
                size = new float[] { extents.x * 2, extents.y * 2, extents.z * 2 },
                rotation = new float[] { rotation.x, rotation.y, rotation.z, rotation.w },
                velocity = null,
                acceleration = null
            };
        }

        static BoxData ConvertToKitti(IdLabelEntry label, uint instanceId, Vector3 center, Vector3 extents, Quaternion rotation)
        {
            return new KittiData
            {
                label_id = label.id,
                label_name = label.label,
                instance_id = instanceId,
                translation = new float[] { center.x, center.y, center.z },
                size = new float[] { extents.x * 2, extents.y * 2, extents.z * 2 },
                yaw = rotation.eulerAngles.y,
            };
        }

        public static GameObject CreateLabeledCube(float scale = 10, string label = "label", float x = 0, float y = 0, float z = 0, float roll = 0, float pitch = 0, float yaw = 0)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, roll));
            cube.transform.localScale = new Vector3(scale, scale, scale);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return cube;
        }

        public void OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                var meshFilters = labeling.gameObject.GetComponentsInChildren<MeshFilter>();

                if (meshFilters == null || meshFilters.Length == 0) return;

                if (idLabelConfig.TryGetLabelEntryFromInstanceId(groundTruthInfo.instanceId, out var labelEntry))
                {
                    var camTrans = perceptionCamera.transform;
                    var transform = labeling.transform;
                    var bounds = new Bounds(Vector3.zero, Vector3.zero);
                    var boundsUnset = true;

                    foreach (var c in Camera.allCameras)
                    {
                        if (c != null && c.isActiveAndEnabled)
                        {
                            var camMatrix = c.worldToCameraMatrix;

                        }
                    }


                    foreach (var mesh in meshFilters)
                    {
                        var currentTransform = mesh.gameObject.transform;
                        var targetTransform = labeling.transform;

                        // Need to copy the bounds because we are going to move them, and do not want to change
                        // the bounds of the mesh
                        var meshBounds = mesh.mesh.bounds;
                        var transformedBounds = new Bounds(meshBounds.center, meshBounds.size);

                        var tmp = currentTransform.TransformPoint(bounds.center);
                        var tmp2 = bounds.center + transform.position;
                        var tmp3 = bounds.center + transform.localPosition;
                        var tmp4 = tmp3 + labeling.transform.position;
                        var tmp5 = tmp3 + labeling.transform.localPosition;
#if false
                        //transformedBounds.center += currentTransform.position;
                        //transformedBounds.extents = Vector3.Scale(currentTransform.localScale, transformedBounds.extents);
                        //transformedBounds.extents = currentTransform.rotation * transformedBounds.extents;
                        transformedBounds.center = currentTransform.TransformPoint(transformedBounds.center);
                        transformedBounds.extents = currentTransform.TransformVector(transformedBounds.extents);
#else
                        while (currentTransform != labeling.transform)
                        {
                            Debug.Log("P: " + currentTransform.position);

                            transformedBounds.center += currentTransform.localPosition;
                            //transformedBounds.extents = Vector3.Scale(currentTransform.localScale, transformedBounds.extents);
                            //transformedBounds.extents = Quaternion.Inverse(currentTransform.rotation) * transformedBounds.extents;
                            //transformedBounds.center = currentTransform.InverseTransformPoint(transformedBounds.center);
                            //transformedBounds.extents = currentTransform.InverseTransformVector(transformedBounds.extents);
                            //transformedBounds.center += currentTransform.Translate(transformedBounds.center);
                            //transformedBounds.center = currentTransform.TransformPoint(transformedBounds.center);
                            transformedBounds.extents = currentTransform.TransformVector(transformedBounds.extents);
                            currentTransform = currentTransform.parent;
                        }
#endif
                        if (boundsUnset)
                        {
                            bounds.center = transformedBounds.center;
                            bounds.extents = transformedBounds.extents;
                            boundsUnset = false;
                        }
                        else
                            bounds.Encapsulate(transformedBounds);
                    }



                    // Need to transform our bounding box by the parent transform, but it should not be rotated, the bounding box should
                    // always be in respect to local transform
                    bounds.center = labeling.transform.TransformPoint(bounds.center);
                    bounds.extents = Vector3.Scale(bounds.extents, labeling.transform.localScale);

                    // Transform the points at the end...

                    var center = bounds.center;
                    var localRotation = Quaternion.Inverse(camTrans.rotation) * transform.rotation;
                    var localCenter = camTrans.InverseTransformPoint(center);
                    localCenter = Vector3.Scale(camTrans.localScale, localCenter);

                    var converted = Convert(labelEntry, groundTruthInfo.instanceId, localCenter, bounds.extents, localRotation, mode);
                    BoundingBoxComputed?.Invoke(m_CurrentFrame, converted);
                    m_BoundingBoxValues[m_CurrentIndex++] = converted;
                }
            }
        }

        public void Working_OnUpdateEntity(Labeling labeling, GroundTruthInfo groundTruthInfo)
        {
            using (s_BoundingBoxCallback.Auto())
            {
                var meshFilters = labeling.gameObject.GetComponentsInChildren<MeshFilter>();

                if (meshFilters == null || meshFilters.Length == 0) return;

                if (idLabelConfig.TryGetLabelEntryFromInstanceId(groundTruthInfo.instanceId, out var labelEntry))
                {
                    var camTrans = perceptionCamera.transform;
                    var transform = labeling.transform;
                    var bounds = new Bounds(Vector3.zero, Vector3.zero);
                    var boundsUnset = true;

                    foreach (var mesh in meshFilters)
                    {
                        // Need to copy the bounds because we are going to move them, and do not want to change
                        // the bounds of the mesh
                        var meshBounds = mesh.mesh.bounds;
                        var transformedBounds = new Bounds(meshBounds.center, meshBounds.size);

                        // Only transform meshes that are not a part of the parent transform...
                        if (mesh.gameObject != labeling.gameObject)
                        {
                            // Properly get the bounds of the mesh with respect to its parent.
                            var meshTrans = mesh.gameObject.transform;
                            transformedBounds.center += meshTrans.localPosition;
                            transformedBounds.extents = Vector3.Scale(meshTrans.localScale, transformedBounds.extents);
                            transformedBounds.extents = meshTrans.localRotation * transformedBounds.extents;
                        }

                        if (boundsUnset)
                        {
                            bounds.center = transformedBounds.center;
                            bounds.extents = transformedBounds.extents;
                            boundsUnset = false;
                        }
                        else
                            bounds.Encapsulate(transformedBounds);
                    }

                    var center = transform.position + bounds.center;
                    var localRotation = Quaternion.Inverse(transform.rotation) * camTrans.rotation;
                    var localCenter = camTrans.InverseTransformPoint(center);
                    var extents =  Vector3.Scale(transform.localScale,bounds.extents);

                    var converted = Convert(labelEntry, groundTruthInfo.instanceId, localCenter, extents, localRotation, mode);
                    BoundingBoxComputed?.Invoke(m_CurrentFrame, converted);
                    m_BoundingBoxValues[m_CurrentIndex++] = converted;
                }
            }
        }

        public void OnEndUpdate()
        {
            perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition).ReportValues(m_BoundingBoxValues);
        }
    }
}
