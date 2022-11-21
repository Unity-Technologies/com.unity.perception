using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;

// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local
namespace UnityEngine.Perception.GroundTruth.Consumers
{
    static class PerceptionJsonFactory
    {
        class PerceptionJsonMessageBuilder : JsonMessageBuilder
        {
            PerceptionEndpoint m_Endpoint;

            public PerceptionJsonMessageBuilder(PerceptionEndpoint endpoint)
            {
                m_Endpoint = endpoint;
            }

            static string GetOrCreateDirectory(string basePath, string key)
            {
                var name = PathUtils.CombineUniversal(basePath, key);

                if (!Directory.Exists(name))
                {
                    Directory.CreateDirectory(name);
                }

                return name;
            }

            public override void AddEncodedImage(string key, string extension, byte[] value)
            {
                var filename = string.Empty;

                if (value.Length > 0)
                {
                    // Get the output directory for labeler, create if necessary, for this we use the key
                    var fName = $"{key}_{m_Endpoint.currentFrame}.{extension.ToLower()}";
                    var path = GetOrCreateDirectory(m_Endpoint.currentPath, key);
                    path = PathUtils.CombineUniversal(path, fName);
                    PathUtils.WriteAndReportImageFile(path, value);
                    m_Endpoint.RegisterFile(path);
                    filename = PathUtils.CombineUniversal(key, fName);
                }

                currentJToken["filename"] = filename;
            }
        }

        public static JToken Convert(PerceptionEndpoint consumer, string id, AnnotationDefinition annotationDefinition)
        {
            switch (annotationDefinition)
            {
                case BoundingBoxDefinition def:
                    return JToken.FromObject(LabelConfigurationAnnotationDefinition.Convert(def, "json", def.spec), consumer.Serializer);
                case BoundingBox3DDefinition def:
                    return JToken.FromObject(LabelConfigurationAnnotationDefinition.Convert(def, "json", def.spec), consumer.Serializer);
                case InstanceSegmentationDefinition def:
                    return JToken.FromObject(LabelConfigurationAnnotationDefinition.Convert(def, "PNG", def.spec), consumer.Serializer);
                case SemanticSegmentationDefinition def:
                    return JToken.FromObject(PerceptionSemanticSegmentationAnnotationDefinition.Convert(def, "PNG"), consumer.Serializer);
                case KeypointAnnotationDefinition kp:
                    return JToken.FromObject(PerceptionKeypointAnnotationDefinition.Convert(consumer, kp), consumer.Serializer);
                default:
                    // If not special case code, use the to message architecture
                    var msgBuilder = new PerceptionJsonMessageBuilder(consumer);
                    annotationDefinition.ToMessage(msgBuilder);
                    return msgBuilder.ToJson();
            }
        }

        public static JToken Convert(PerceptionEndpoint consumer, string id, MetricDefinition def)
        {
            switch (def)
            {
                case ObjectCountMetricDefinition casted:
                    return JToken.FromObject(LabelConfigMetricDefinition.Convert(id, def, casted.spec), consumer.Serializer);
                case RenderedObjectInfoMetricDefinition casted:
                    return JToken.FromObject(LabelConfigMetricDefinition.Convert(id, def, casted.spec), consumer.Serializer);
                default:
                    // If not special case code, use the to message architecture
                    var msgBuilder = new PerceptionJsonMessageBuilder(consumer);
                    def.ToMessage(msgBuilder);
                    return msgBuilder.ToJson();
            }
        }

        public static string GetNameFromAnnotationDefinition(AnnotationDefinition annotationDefinition)
        {
            return annotationDefinition switch
            {
                BoundingBoxDefinition def => "bounding box",
                BoundingBox3DDefinition def => "bounding box 3D",
                InstanceSegmentationDefinition def => "instance segmentation",
                SemanticSegmentationDefinition def => "semantic segmentation",
                KeypointAnnotationDefinition def => "keypoints",
                _ => annotationDefinition.GetType().ToString()
            };
        }

        internal static string WriteOutCapture(PerceptionEndpoint consumer, Frame frame, RgbSensor sensor)
        {
            var path = consumer.WriteOutImageFile(frame.frame, sensor);
            return consumer.RemoveDatasetPathPrefix(path);
        }

        public static JToken Convert(PerceptionEndpoint consumer, Frame frame, RgbSensor sensor)
        {
            var path = consumer.WriteOutImageFile(frame.frame, sensor);
            var convertedSensor = PerceptionRgbSensor.Convert(sensor);
            return JToken.FromObject(convertedSensor, consumer.Serializer);
        }

        public static JToken Convert(PerceptionEndpoint consumer, Frame frame, string labelerId, string defId, Annotation annotation)
        {
            switch (annotation)
            {
                case InstanceSegmentationAnnotation i:
                {
                    return JToken.FromObject(PerceptionInstanceSegmentationValue.Convert(consumer, frame.frame, i), consumer.Serializer);
                }
                case BoundingBoxAnnotation b:
                {
                    return JToken.FromObject(PerceptionBoundingBoxAnnotationValue.Convert(consumer, labelerId, defId, b), consumer.Serializer);
                }
                case BoundingBox3DAnnotation b:
                {
                    return JToken.FromObject(PerceptionBounding3dBoxAnnotationValue.Convert(consumer, labelerId, defId, b), consumer.Serializer);
                }
                case SemanticSegmentationAnnotation s:
                {
                    return JToken.FromObject(PerceptionSemanticSegmentationValue.Convert(consumer, frame.frame, s), consumer.Serializer);
                }
                case KeypointAnnotation kp:
                {
                    return JToken.FromObject(PerceptionKeyPointValue.Convert(consumer, kp), consumer.Serializer);
                }
                default:
                {
                    // If not special case code, use the to message architecture
                    var msgBuilder = new PerceptionJsonMessageBuilder(consumer);
                    annotation.ToMessage(msgBuilder);
                    return msgBuilder.ToJson();
                }
            }
        }
    }

    [Serializable]
    struct LabelConfigMetricDefinition
    {
        LabelConfigMetricDefinition(string id, string name, string description, IdLabelConfig.LabelEntrySpec[] spec)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.spec = spec;
        }

        public string id;
        public string name;
        public string description;
        public IdLabelConfig.LabelEntrySpec[] spec;

        public JToken ToJToken()
        {
            return JToken.FromObject(this);
        }

        public static LabelConfigMetricDefinition Convert(string id, MetricDefinition def, IdLabelConfig.LabelEntrySpec[] spec)
        {
            return new LabelConfigMetricDefinition(id, def.id, def.description, spec);
        }
    }

    [Serializable]
    struct LabelConfigurationAnnotationDefinition
    {
        public string id;
        public string name;
        public string description;
        public string format;
        public IdLabelConfig.LabelEntrySpec[] spec;

        LabelConfigurationAnnotationDefinition(AnnotationDefinition def, string format, IdLabelConfig.LabelEntrySpec[] spec)
        {
            id = def.id;
            name = PerceptionJsonFactory.GetNameFromAnnotationDefinition(def);
            description = def.description;
            this.format = format;
            this.spec = spec;
        }

        public static LabelConfigurationAnnotationDefinition Convert(AnnotationDefinition def, string format, IdLabelConfig.LabelEntrySpec[] spec)
        {
            return new LabelConfigurationAnnotationDefinition(def, format, spec);
        }
    }

    [Serializable]
    struct GenericMetricDefinition
    {
        public string id;
        public string name;
        public string description;

        public GenericMetricDefinition(string id, MetricDefinition def)
        {
            this.id = id;
            name = def.id;
            description = def.description;
        }

        public static GenericMetricDefinition Convert(string id, MetricDefinition def)
        {
            return new GenericMetricDefinition(id, def);
        }
    }

    [Serializable]
    struct PerceptionKeyPointValue
    {
        public string id;
        public string annotation_definition;
        public List<Entry> values;

        [Serializable]
        internal struct Keypoint
        {
            public int index;
            public float x;
            public float y;
            public float camera_x;
            public float camera_y;
            public float camera_z;
            public int state;

            Keypoint(KeypointValue kp)
            {
                index = kp.index;
                x = kp.location.x;
                y = kp.location.y;
                camera_x = kp.cameraCartesianLocation.x;
                camera_y = kp.cameraCartesianLocation.y;
                camera_z = kp.cameraCartesianLocation.z;
                state = kp.state;
            }

            public static Keypoint Convert(KeypointValue kp)
            {
                return new Keypoint(kp);
            }
        }

        [Serializable]
        internal struct Entry
        {
            public int label_id;
            public uint instance_id;
            public string template_guid;
            public string pose;
            public Keypoint[] keypoints;

            Entry(string template, KeypointComponent entry)
            {
                label_id = entry.labelId;
                instance_id = entry.instanceId;
                template_guid = template;
                pose = entry.pose;
                keypoints = entry.keypoints.Select(Keypoint.Convert).ToArray();
            }

            public static Entry Convert(string template, KeypointComponent entry)
            {
                return new Entry(template, entry);
            }
        }

        PerceptionKeyPointValue(KeypointAnnotation kp)
        {
            id = kp.id;
            annotation_definition = kp.annotationId;
            values = new List<Entry>();
            foreach (var i in kp.entries)
            {
                values.Add(Entry.Convert(kp.templateId, i));
            }
        }

        public static PerceptionKeyPointValue Convert(PerceptionEndpoint consumer, KeypointAnnotation kp)
        {
            return new PerceptionKeyPointValue(kp);
        }
    }

    [Serializable]
    struct PerceptionSemanticSegmentationValue
    {
        public string id;
        public string annotation_definition;
        public string filename;

        static string CreateFile(PerceptionEndpoint consumer, int frame, SemanticSegmentationAnnotation annotation)
        {
            var path = PathUtils.CombineUniversal(consumer.GetProductPath(annotation), $"segmentation_{frame}.png");
            PathUtils.WriteAndReportImageFile(path, annotation.buffer);
            consumer.RegisterFile(path);
            return path;
        }

        public static PerceptionSemanticSegmentationValue Convert(PerceptionEndpoint consumer, int frame, SemanticSegmentationAnnotation annotation)
        {
            return new PerceptionSemanticSegmentationValue
            {
                id = annotation.id,
                annotation_definition = annotation.annotationId,
                filename = consumer.RemoveDatasetPathPrefix(CreateFile(consumer, frame, annotation)),
            };
        }
    }


    [Serializable]
    struct PerceptionRgbSensor
    {
        public string sensor_id;
        public string ego_id;
        public string modality;
        public Vector3 translation;
        public Quaternion rotation;
        public float[][] camera_intrinsic;
        public string projection;

        static float[][] ToFloatArray(float3x3 inF3)
        {
            return new[]
            {
                new[] { inF3[0][0], inF3[0][1], inF3[0][2] },
                new[] { inF3[1][0], inF3[1][1], inF3[1][2] },
                new[] { inF3[2][0], inF3[2][1], inF3[2][2] }
            };
        }

        public static PerceptionRgbSensor Convert(RgbSensor inRgb)
        {
            return new PerceptionRgbSensor
            {
                sensor_id = inRgb.id,
                ego_id = "ego",
                modality = "camera",
                translation = inRgb.position,
                rotation = inRgb.rotation,
                projection = inRgb.projection.ToString().ToLower(),
                camera_intrinsic = ToFloatArray(inRgb.matrix)
            };
        }
    }

    [Serializable]
    struct PerceptionInstanceSegmentationValue
    {
        internal struct Entry
        {
            public int instance_id;
            public int labelId;
            public string labelName;
            public Color32 color;

            Entry(InstanceSegmentationEntry entry)
            {
                instance_id = entry.instanceId;
                color = entry.color;
                labelId = entry.labelId;
                labelName = entry.labelName;
            }

            internal static Entry Convert(InstanceSegmentationEntry entry)
            {
                return new Entry(entry);
            }
        }

        public string id;
        public string annotation_definition;
        public string filename;
        public List<Entry> values;

        PerceptionInstanceSegmentationValue(PerceptionEndpoint consumer, int frame, InstanceSegmentationAnnotation annotation)
        {
            id = annotation.id;
            annotation_definition = annotation.annotationId;
            filename = consumer.RemoveDatasetPathPrefix(CreateFile(consumer, frame, annotation));
            values = annotation.instances.Select(Entry.Convert).ToList();
        }

        static string CreateFile(PerceptionEndpoint consumer, int frame, InstanceSegmentationAnnotation annotation)
        {
            var path = PathUtils.CombineUniversal(consumer.GetProductPath(annotation), $"Instance_{frame}.png");
            PathUtils.WriteAndReportImageFile(path, annotation.buffer);
            consumer.RegisterFile(path);
            return path;
        }

        public static PerceptionInstanceSegmentationValue Convert(PerceptionEndpoint consumer, int frame, InstanceSegmentationAnnotation annotation)
        {
            return new PerceptionInstanceSegmentationValue(consumer, frame, annotation);
        }
    }

    [Serializable]
    struct PerceptionBounding3dBoxAnnotationValue
    {
        [Serializable]
        internal struct BBVector3
        {
            public BBVector3(Vector3 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }

            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        internal struct BBQuaternion
        {
            public BBQuaternion(Quaternion q)
            {
                x = q.x;
                y = q.y;
                z = q.z;
                w = q.w;
            }

            public float x;
            public float y;
            public float z;
            public float w;
        }

        [Serializable]
        internal struct Entry
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
            public BBVector3 translation;
            public BBVector3 size;
            public BBQuaternion rotation;
            public BBVector3 velocity;
            public BBVector3 acceleration;

            Entry(BoundingBox3D entry)
            {
                label_id = entry.labelId;
                label_name = entry.labelName;
                instance_id = entry.instanceId;
                translation = new BBVector3(entry.translation);
                size = new BBVector3(entry.size);
                rotation = new BBQuaternion(entry.rotation);
                velocity = new BBVector3(entry.velocity);
                acceleration = new BBVector3(entry.acceleration);
            }

            internal static Entry Convert(BoundingBox3D entry)
            {
                return new Entry(entry);
            }
        }

        public string id;
        public string annotation_definition;
        public List<Entry> values;

        PerceptionBounding3dBoxAnnotationValue(string labelerId, string defId, BoundingBox3DAnnotation annotation)
        {
            id = labelerId;
            annotation_definition = defId;
            values = annotation.boxes.Select(Entry.Convert).ToList();
        }

        public static PerceptionBounding3dBoxAnnotationValue Convert(PerceptionEndpoint consumer, string labelerId, string defId, BoundingBox3DAnnotation annotation)
        {
            return new PerceptionBounding3dBoxAnnotationValue(labelerId, defId, annotation);
        }
    }

    [Serializable]
    struct PerceptionSemanticSegmentationAnnotationDefinition
    {
        internal struct Entry
        {
            public string label_name;
            public Color pixel_value;

            Entry(SemanticSegmentationDefinitionEntry e)
            {
                label_name = e.labelName;
                pixel_value = e.pixelValue;
            }

            internal static Entry Convert(SemanticSegmentationDefinitionEntry e)
            {
                return new Entry(e);
            }
        }

        public string id;
        public string name;
        public string description;
        public string format;
        public List<Entry> spec;

        PerceptionSemanticSegmentationAnnotationDefinition(SemanticSegmentationDefinition def, string format)
        {
            id = def.id;
            name = PerceptionJsonFactory.GetNameFromAnnotationDefinition(def);
            description = def.description;
            spec = def.spec.Select(Entry.Convert).ToList();
            this.format = format;
        }

        public static PerceptionSemanticSegmentationAnnotationDefinition Convert(SemanticSegmentationDefinition def, string format)
        {
            return new PerceptionSemanticSegmentationAnnotationDefinition(def, format);
        }
    }

    [Serializable]
    struct PerceptionKeypointAnnotationDefinition
    {
        [Serializable]
        internal struct JointJson
        {
            public string label;
            public int index;
            public Color32 color;

            internal static JointJson Convert(KeypointAnnotationDefinition.JointDefinition joint)
            {
                return new JointJson
                {
                    label = joint.label,
                    index = joint.index,
                    color = joint.color
                };
            }
        }

        [Serializable]
        internal struct SkeletonJson
        {
            public int joint1;
            public int joint2;
            public Color32 color;

            internal static SkeletonJson Convert(KeypointAnnotationDefinition.SkeletonDefinition skeleton)
            {
                return new SkeletonJson
                {
                    joint1 = skeleton.joint1,
                    joint2 = skeleton.joint2,
                    color = skeleton.color
                };
            }
        }

        [Serializable]
        internal struct KeypointJson
        {
            public string template_id;
            public string template_name;
            public JointJson[] key_points;
            public SkeletonJson[] skeleton;

            internal static KeypointJson Convert(KeypointAnnotationDefinition.Template e)
            {
                return new KeypointJson
                {
                    template_id = e.templateId,
                    template_name = e.templateName,
                    key_points = e.keyPoints.Select(JointJson.Convert).ToArray(),
                    skeleton = e.skeleton.Select(SkeletonJson.Convert).ToArray()
                };
            }
        }

        public string id;
        public string name;
        public string description;
        public string format;
        public List<KeypointJson> spec;

        public static PerceptionKeypointAnnotationDefinition Convert(PerceptionEndpoint consumer, KeypointAnnotationDefinition def)
        {
            return new PerceptionKeypointAnnotationDefinition
            {
                id = def.id,
                name = PerceptionJsonFactory.GetNameFromAnnotationDefinition(def),
                description = def.description,
                format = "json",
                spec = new List<KeypointJson>()
                {
                    KeypointJson.Convert(def.template)
                }
            };
        }
    }

    [Serializable]
    struct PerceptionBoundingBoxAnnotationValue
    {
        [Serializable]
        internal struct Entry
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
            public float x;
            public float y;
            public float width;
            public float height;

            internal static Entry Convert(BoundingBox entry)
            {
                return new Entry
                {
                    label_id = entry.labelId,
                    label_name = entry.labelName,
                    instance_id = (uint)entry.instanceId,
                    x = entry.origin.x,
                    y = entry.origin.y,
                    width = entry.dimension.x,
                    height = entry.dimension.y
                };
            }
        }

        public string id;
        public string annotation_definition;
        public List<Entry> values;

        public static PerceptionBoundingBoxAnnotationValue Convert(PerceptionEndpoint consumer, string labelerId, string defId, BoundingBoxAnnotation annotation)
        {
            return new PerceptionBoundingBoxAnnotationValue
            {
                id = labelerId,
                annotation_definition = defId,
                values = annotation.boxes.Select(Entry.Convert).ToList()
            };
        }
    }
}
