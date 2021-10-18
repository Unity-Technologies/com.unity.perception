using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Simulation;
using UnityEngine.Assertions;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Analytics
{
    #region Common

    enum AnalyticsEventType
    {
        Runtime,
        Editor,
        RuntimeAndEditor
    }
    struct AnalyticsEvent
    {
        public string name { get; private set; }
        public AnalyticsEventType type { get; private set; }
        public int versionId { get; private set; }
        public string prefix { get; private set; }

        public AnalyticsEvent(string name, AnalyticsEventType type, int versionId, string prefix = "")
        {
            this.name = name;
            this.type = type;
            this.versionId = versionId;
            this.prefix = prefix;
        }
    }

    #endregion

    #region Run In Unity Simulation

    [Serializable]
    enum RunStatus
    {
        Started,
        Failed,
        Succeeded
    }

    [Serializable]
    struct RunInUnitySimulationData
    {
        public string runId;
        public int totalIterations;
        public int instanceCount;
        public string existingBuildId;
        public string errorMessage;
        public string runExecutionId;
        public string runStatus;
    }

    #endregion

    #region Scenario Information

    [Serializable]
    public class PerceptionCameraData
    {
        public string captureTriggerMode;
        // same as "firstCaptureFrame" of the Perception Camera
        public int startAtFrame;
        public int framesBetweenCaptures;
    }

    [Serializable]
    public class LabelerData
    {
        public string name;
        public int labelConfigCount;
        public string objectFilter = "";
        public int animationPoseCount;

        public static LabelerData FromLabeler(CameraLabeler labeler)
        {
            var labelerType = labeler.GetType();
            var labelerName = labelerType.Name;
            if (!PerceptionAnalytics.labelerAllowList.Contains(labelerName))
                return null;

            var labelerData = new LabelerData()
            {
                name = labelerName
            };

            switch (labeler)
            {
                case BoundingBox3DLabeler bb3dl:
                    labelerData.labelConfigCount = bb3dl.idLabelConfig.labelEntries.Count;
                    break;
                case BoundingBox2DLabeler bb2dl:
                    labelerData.labelConfigCount = bb2dl.idLabelConfig.labelEntries.Count;
                    break;
                case InstanceSegmentationLabeler isl:
                    labelerData.labelConfigCount = isl.idLabelConfig.labelEntries.Count;
                    break;
                case KeypointLabeler kpl:
                    labelerData.labelConfigCount = kpl.idLabelConfig.labelEntries.Count;
                    labelerData.objectFilter = kpl.objectFilter.ToString();
                    labelerData.animationPoseCount = kpl.animationPoseConfigs.Count;
                    break;
                case ObjectCountLabeler ocl:
                    labelerData.labelConfigCount = ocl.labelConfig.labelEntries.Count;
                    break;
                case SemanticSegmentationLabeler ssl:
                    labelerData.labelConfigCount = ssl.labelConfig.labelEntries.Count;
                    break;
                case RenderedObjectInfoLabeler rol:
                    labelerData.labelConfigCount = rol.idLabelConfig.labelEntries.Count;
                    break;
                default:
                    labelerData = null;
                    break;
            }

            return labelerData;
        }
    }

    [Serializable]
    public class MemberData
    {
        public string name;
        public string value;
        public string type;
    }

    [Serializable]
    public class ParameterField
    {
        public string name;
        public string distribution;
        public float value;
        public float rangeMinimum;
        public float rangeMaximum;
        public float mean;
        public float stdDev;
        public int categoricalParameterCount;

        public static ParameterField ExtractSamplerInformation(ISampler sampler, string fieldName = "value")
        {
            switch (sampler)
            {
                case AnimationCurveSampler _:
                    return new ParameterField()
                    {
                        name = fieldName,
                        distribution = "AnimationCurve"
                    };
                case ConstantSampler cs:
                    return new ParameterField()
                    {
                        name = fieldName,
                        distribution = "Constant",
                        value = cs.value
                    };
                case NormalSampler ns:
                    return new ParameterField()
                    {
                        name = fieldName,
                        distribution = "Normal",
                        mean = ns.mean,
                        stdDev = ns.standardDeviation,
                        rangeMinimum = ns.range.minimum,
                        rangeMaximum = ns.range.maximum
                    };
                case UniformSampler us:
                    return new ParameterField()
                    {
                        name = fieldName,
                        distribution = "Uniform",
                        rangeMinimum = us.range.minimum,
                        rangeMaximum = us.range.maximum
                    };
                default:
                    return null;
            }
        }

        public static List<ParameterField> ExtractSamplersInformation(IEnumerable<ISampler> samplers, IEnumerable<string> indexToNameMapping)
        {
            var samplersList = samplers.ToList();
            var indexToNameMappingList = indexToNameMapping.ToList();
            if (samplersList.Count > indexToNameMappingList.Count)
                throw new Exception("Insufficient names provided for mapping ParameterFields");
            return samplersList.Select((t, i) => ExtractSamplerInformation(t, indexToNameMappingList[i])).ToList();
        }
    }

    [Serializable]
    public class ParameterData
    {
        public string name;
        public string type;
        public List<ParameterField> fields;
    }

    [Serializable]
    public class RandomizerData
    {
        public string name;
        public MemberData[] members;
        public ParameterData[] parameters;

        public static RandomizerData FromRandomizer(Randomizer randomizer)
        {
            if (randomizer == null)
                return null;

            var data = new RandomizerData()
            {
                name = randomizer.GetType().Name,
            };

            var randomizerType = randomizer.GetType();

            // Filter out randomizers which could be considered personally identifiable.
            if (randomizerType.Namespace == null || !randomizerType.Namespace.StartsWith("UnityEngine.Perception"))
                return null;

            // Naming configuration
            var vectorFieldNames = new[] { "x", "y", "z", "w" };
            var hsvaFieldNames = new[] { "hue", "saturation", "value", "alpha" };
            var rgbFieldNames = new[] { "red", "green", "blue" };

            // Add member fields and parameters separately
            var members = new List<MemberData>();
            var parameters = new List<ParameterData>();
            foreach (var field in randomizerType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var member = field.GetValue(randomizer);
                var memberType = member.GetType();

                // If member is either a categorical or numeric parameter
                if (memberType.IsSubclassOf(typeof(Parameter)))
                {
                    var pd = new ParameterData()
                    {
                        name = field.Name,
                        type = memberType.Name,
                        fields = new List<ParameterField>()
                    };

                    // All included parameter types
                    switch (member)
                    {
                        case CategoricalParameterBase cp:
                            pd.fields.Add(new ParameterField()
                            {
                                name = "values",
                                distribution = "Categorical",
                                categoricalParameterCount = cp.probabilities.Count
                            });
                            break;
                        case BooleanParameter bP:
                            pd.fields.Add(ParameterField.ExtractSamplerInformation(bP.value));
                            break;
                        case FloatParameter fP:
                            pd.fields.Add(ParameterField.ExtractSamplerInformation(fP.value));
                            break;
                        case IntegerParameter iP:
                            pd.fields.Add(ParameterField.ExtractSamplerInformation(iP.value));
                            break;
                        case Vector2Parameter v2P:
                            pd.fields.AddRange(ParameterField.ExtractSamplersInformation(v2P.samplers, vectorFieldNames));
                            break;
                        case Vector3Parameter v3P:
                            pd.fields.AddRange(ParameterField.ExtractSamplersInformation(v3P.samplers, vectorFieldNames));
                            break;
                        case Vector4Parameter v4P:
                            pd.fields.AddRange(ParameterField.ExtractSamplersInformation(v4P.samplers, vectorFieldNames));
                            break;
                        case ColorHsvaParameter hsvaP:
                            pd.fields.AddRange(ParameterField.ExtractSamplersInformation(hsvaP.samplers, hsvaFieldNames));
                            break;
                        case ColorRgbParameter rgbP:
                            pd.fields.AddRange(ParameterField.ExtractSamplersInformation(rgbP.samplers, rgbFieldNames));
                            break;
                    }

                    parameters.Add(pd);
                }
                else
                {
                    members.Add(new MemberData()
                    {
                        name = field.Name,
                        type = memberType.FullName,
                        value = member.ToString()
                    });
                }
            }

            data.members = members.ToArray();
            data.parameters = parameters.ToArray();
            return data;
        }
    }

    [Serializable]
    public class ScenarioCompletedData
    {
        public string platform;
        public PerceptionCameraData perceptionCamera;
        public LabelerData[] labelers;
        public RandomizerData[] randomizers;

        internal static ScenarioCompletedData FromCameraAndRandomizers(
            PerceptionCamera cam,
            IEnumerable<Randomizer> randomizers
        )
        {
            var data = new ScenarioCompletedData()
            {
                platform = (Configuration.Instance.IsSimulationRunningInCloud()) ? "USim": Application.platform.ToString()
            };

            if (cam != null)
            {
                // Perception Camera Data
                data.perceptionCamera = new PerceptionCameraData()
                {
                    captureTriggerMode = cam.captureTriggerMode.ToString(),
                    startAtFrame = cam.firstCaptureFrame,
                    framesBetweenCaptures = cam.framesBetweenCaptures
                };

                // Labeler Data
                data.labelers = cam.labelers
                    .Select(LabelerData.FromLabeler)
                    .Where(labeler => labeler != null).ToArray();
            }

            var randomizerList = randomizers.ToArray();
            if (randomizerList.Any())
            {
                data.randomizers = randomizerList
                    .Select(RandomizerData.FromRandomizer)
                    .Where(x => x != null).ToArray();
            }
            else
            {
                data.randomizers = new RandomizerData[] { };
            }

            return data;
        }
    }

    #endregion
}
