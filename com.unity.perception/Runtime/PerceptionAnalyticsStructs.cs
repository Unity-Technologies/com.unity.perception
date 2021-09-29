using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Analytics
{
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
        public int startAtFrame;
        public int framesBetweenCaptures;
    }

    [Serializable]
    public class LabelerData
    {
        public string name;
        public int labelConfigCount;
        public string objectFilter;
        public int animationPoseCount;

        public static LabelerData FromLabeler(CameraLabeler labeler)
        {
            var labelerType = labeler.GetType();
            if (!PerceptionAnalytics.labelerAllowList.Contains(labelerType))
                return null;

            var labelerData = new LabelerData()
            {
                name = labeler.GetType().Name
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
        public float value = int.MinValue;
        public float rangeMinimum = int.MinValue;
        public float rangeMaximum = int.MinValue;
        public float mean = int.MinValue;
        public float stdDev = int.MinValue;
        public int categoricalParameterCount = int.MinValue;

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

            // Only looks for randomizers included by the Perception package.
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
    }

    #endregion
}
