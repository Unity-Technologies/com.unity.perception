using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        Dictionary<string, GameObject> boundsMap = new Dictionary<string, GameObject>();

        Vector3[] GetBoxCorners(Bounds bounds, Quaternion q)
        {
#if false
            
            
            var c = bounds.center;
            var ll = c - bounds.extents;
            var ur = c + bounds.extents;
#else
            var c = bounds.center;
            var r = Vector3.right * bounds.extents.x;
            var u = Vector3.up * bounds.extents.y;
            var f = Vector3.forward * bounds.extents.z;

            r = q * r;
            u = q * u;
            f = q * f;
            
            var r2 = r * 2;
            var u2 = u * 2;
            var f2 = f * 2;
            
            var cs = new Vector3[8];
            cs[0] = c - r - u - f;
            cs[1] = cs[0] + u2;
            cs[2] = cs[1] + r2;
            cs[3] = cs[0] + r2; 
            for (int i = 0; i < 4; i++)
            {
                cs[i + 4] = cs[i] + f2;
            }

            return cs;
#endif
        }
        
        public GameObject boundingBoxVizPrefab = null;
        GameObject bbViz = null;

        Bounds TransformBounds(Vector3 pos, Matrix4x4 matrix, Bounds bounds)
        {
            var aMin = bounds.min;
            var aMax = bounds.max;

            var bMin = bounds.center;
            var bMax = bounds.center;
            
            float a, b;
            
            // Now find the extreme points by considering the product of the min and max with each component
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    a = matrix[i, j] * aMin[j];
                    b = matrix[i, j] * aMax[j];

                    if (a < b)
                    {
                        bMin[i] += a;
                        bMax[i] += b;
                    }
                    else
                    {
                        bMin[i] += b;
                        bMax[i] += a;
                    }
                }
            }

            var ext = (bMax - bMin) * 0.5f;
            var center = bMin + ext;
            return new Bounds(center, ext);
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
#if false
                    // Set up my bound visuals
                    foreach (var mesh in meshFilters)
                    {
                        var currentTransform = mesh.gameObject.transform;

                        // Need to copy the bounds because we are going to move them, and do not want to change
                        // the bounds of the mesh
                        var meshBounds = mesh.mesh.bounds;
                        
                        if (boundsMap == null) boundsMap = new Dictionary<string, GameObject>();

                        if (!boundsMap.ContainsKey(mesh.gameObject.name))
                        {
                            boundsMap[mesh.gameObject.name] = Object.Instantiate(boundingBoxVizPrefab);
                            boundsMap[mesh.gameObject.name].name = mesh.gameObject.name + "_bounds";
                        }
                        var b = boundsMap[mesh.gameObject.name];
                        
                        b.transform.localPosition = mesh.transform.TransformPoint(meshBounds.center);
                        //b.transform.localRotation = Quaternion.Inverse(mesh.transform.rotation);
                        b.transform.localRotation = mesh.transform.rotation;
                        b.transform.localScale = Vector3.Scale(meshBounds.extents, mesh.transform.localScale) * 2;
                    }
#endif           
                    // Need to convert all bounds into labeling mesh space...
                    foreach (var mesh in meshFilters)
                    {
                        var currentTransform = mesh.gameObject.transform;

                        var meshBounds = mesh.mesh.bounds;
                        
                        // Need to copy the bounds because we are going to move them, and do not want to change
                        // the bounds of the mesh
                        
                        //var transformedBounds = new Bounds(currentTransform.localPosition + meshBounds.center, meshBounds.size * 2);
                        var transformedBounds = new Bounds(meshBounds.center, meshBounds.size);// * 2);
                        var transformedRotation = Quaternion.identity;
                        
                        // Convert all sub-components bounds into top level component's space
                        while (currentTransform != labeling.transform)
                        {
                            transformedBounds.center += currentTransform.localPosition;// currentTransform.localRotation * currentTransform.localPosition;// + transformedBounds.center;
                            //transformedBounds.extents = currentTransform.localRotation * transformedBounds.extents;
                            //transformedBounds.extents = Vector3.Scale(transformedBounds.extents, currentTransform.localScale);
                            transformedRotation *= currentTransform.localRotation;
                            currentTransform = currentTransform.parent;
                        }

                        //transformedBounds.center += currentTransform.localPosition;
                        
                        //var center = currentTransform.TransformPoint(meshBounds.center);
                        //var scale = Vector3.Scale(meshBounds.extents, currentTransform.localScale) * 2;
#if true
                        //var cs = GetBoxCorners(new Bounds(center, scale * 2), Quaternion.identity);
                        var cs = GetBoxCorners(transformedBounds, transformedRotation);//Quaternion.identity);//
                        
                        // Merge the bounds of the current child with the the entire component's bounds
                        if (boundsUnset)
                        {
                            bounds = new Bounds(cs[0], Vector3.zero);
                            boundsUnset = false;
                        }

                        foreach (var c2 in cs)
                        {
                            bounds.Encapsulate(c2);
                        }
#else
                        if (boundsUnset)
                        {
                            bounds = new Bounds(transformedBounds.center, transformedBounds.size);
                            boundsUnset = false;
                        }
                        else
                        {
                            bounds.Encapsulate(transformedBounds);
                            
                        }
#endif
                    }

                    var labelTransform = labeling.transform;

                    var testBounds = new Bounds(Vector3.zero, Vector3.zero);

                    var rotTowards = Quaternion.identity;

                    // Convert the encapsulated model's bounds into world space
                    var localRotation = Quaternion.Inverse(camTrans.rotation) * labelTransform.rotation;

                    bounds.center = labelTransform.TransformPoint(bounds.center);
                    bounds.extents = Vector3.Scale(bounds.extents,  labelTransform.localScale);

                    #if true
                    if (bbViz == null) bbViz = Object.Instantiate(boundingBoxVizPrefab);
                    
                    bbViz.transform.position = bounds.center ;
                    bbViz.transform.localRotation = localRotation;// * Quaternion.Inverse(rotTowards);
                    bbViz.transform.localScale = bounds.extents * 2;// * 2f;
                    #endif
                   

                    // Now convert all points into camera's space
                   
                    var localCenter = camTrans.InverseTransformPoint(bounds.center);
                    localCenter = Vector3.Scale(camTrans.localScale, localCenter);
                    
                    var converted = ConvertToBoxData(labelEntry, groundTruthInfo.instanceId, localCenter, bounds.extents, localRotation);
                    BoundingBoxComputed?.Invoke(m_CurrentFrame, converted);
                    m_BoundingBoxValues.Add(converted);
                }
            }
        }
        
        
        
        #if false
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

                        if (boundsMap == null) boundsMap = new Dictionary<string, GameObject>();

                        if (!boundsMap.ContainsKey(mesh.gameObject.name))
                        {
                            boundsMap[mesh.gameObject.name] = Object.Instantiate(boundingBoxVizPrefab);
                            boundsMap[mesh.gameObject.name].name = mesh.gameObject.name + "_bounds";
                        }
                        var b = boundsMap[mesh.gameObject.name];
                        
                        b.transform.localPosition = mesh.transform.TransformPoint(meshBounds.center);
                        //b.transform.localRotation = Quaternion.Inverse(mesh.transform.rotation);
                        b.transform.localRotation = mesh.transform.rotation;
                        b.transform.localScale = Vector3.Scale(meshBounds.extents, mesh.transform.localScale) * 2;
                        
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
                            bounds.center = currentTransform.localPosition;
                            bounds.extents = transformedBounds.extents;
                            boundsUnset = false;
                        }
                        else
                            bounds.Encapsulate(transformedBounds);
                    }


                    var labelTransform = labeling.transform;

                    var testBounds = new Bounds(Vector3.zero, Vector3.zero);

                    var rotTowards = Quaternion.identity;

                    bool firstTime = true;
                    
                    rotTowards.SetFromToRotation(labeling.transform.forward, Vector3.forward);
                    var lookAt = Quaternion.Inverse(labeling.transform.rotation);
                    foreach (var i in boundsMap.Values)
                    {

#if false
                        // transform bounds in labeling coordinates space
                        var c = labelTransform.InverseTransformPoint(i.transform.localPosition);
                        var r = labelTransform.InverseTransformVector(i.transform.localScale);
#else
                        var c = i.transform.position;
                        //var r = rotTowards * i.transform.localScale;
                        var r = i.transform.localScale;// * 2;
#endif
                        if (firstTime)
                        {
                            testBounds = new Bounds(c, r);
                            var cs = GetBoxCorners(testBounds, lookAt * i.transform.rotation);
                            
                            testBounds = new Bounds(cs[0], Vector3.zero);
                            foreach (var c2 in cs)
                            {
                                testBounds.Encapsulate(c2);
                            }
                            
                            firstTime = false;
                        }
                        else
                        {
                            //var tmp = TransformBounds(Vector3.zero, i.transform.localToWorldMatrix, new Bounds(c, r));
                            //testBounds = CombineBounds(testBounds, new Bounds(c, r));
                            //testBounds = CombineBounds(testBounds, tmp);
                            
                            var nb = new Bounds(c, r);
                            var cs = GetBoxCorners(nb, lookAt * i.transform.rotation);
                            foreach (var c2 in cs)
                            {
                                testBounds.Encapsulate(c2);
                            }

                            //#if false
                            //if (!testBounds.Contains(c + r))
                            //{
                            //    testBounds.Encapsulate(c + r);
                            //}
                            //if (!testBounds.Contains(c - r))
                            //{
                            //    testBounds.Encapsulate(c - r);
                            //}

                            //testBounds.Encapsulate((c + testBounds.center) + r);

                            //testBounds.Encapsulate((c + testBounds.center) - r);
                            //testBounds.Encapsulate(c - r);
//#endif
                        }
                    }
                    
                    
                    
                    // Convert the encapsulated model's bounds into world space
                    
                    bounds.center = labelTransform.TransformPoint(bounds.center);
                    bounds.extents = Vector3.Scale(bounds.extents,  labelTransform.localScale);

                    if (bbViz == null) bbViz = Object.Instantiate(boundingBoxVizPrefab);
                    
                    // Now convert all points into camera's space
                    var localRotation = Quaternion.Inverse(camTrans.rotation) * labelTransform.rotation;
                    var localCenter = camTrans.InverseTransformPoint(bounds.center);
                    localCenter = Vector3.Scale(camTrans.localScale, localCenter);

                    // Visualize
                    
                    // Convert viz components back into world space
                    
                    bbViz.transform.position = testBounds.center;
                    bbViz.transform.localRotation = localRotation;// * Quaternion.Inverse(rotTowards);
                    bbViz.transform.localScale = testBounds.extents * 2f;

             //       foreach (var i in boundsMap)
             //       {
             //           var t = i.Value.transform;
             //           t.localPosition = camTrans.InverseTransformPoint(t.localPosition);
             //           t.localRotation = Qu
             //       }
                    
                    
                    var converted = ConvertToBoxData(labelEntry, groundTruthInfo.instanceId, localCenter, bounds.extents, localRotation);
                    BoundingBoxComputed?.Invoke(m_CurrentFrame, converted);
                    m_BoundingBoxValues.Add(converted);
                }
            }
        }
#endif
        Bounds CombineBounds(Bounds a, Bounds b)
        {
            var minx = Math.Min(a.min.x, b.min.x);
            var miny = Math.Min(a.min.y, b.min.y);
            var minz = Math.Min(a.min.z, b.min.z);
            var maxx = Math.Max(a.max.x, b.max.x);
            var maxy = Math.Max(a.max.y, b.max.y);
            var maxz = Math.Max(a.max.z, b.max.z);
            var ret = new Bounds();
            ret.SetMinMax(new Vector3(minx, miny, minz), new Vector3(maxx,maxy, maxz) );
            return ret;
        }
        
        /// <inheritdoc/>
        public void OnEndUpdate()
        {
            perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition).ReportValues(m_BoundingBoxValues);
        }
    }
}
