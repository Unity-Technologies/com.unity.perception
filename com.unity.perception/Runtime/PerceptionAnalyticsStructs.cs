using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.GroundTruth.Sensors;
using UnityEngine.Perception.Randomization.Scenarios;

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

    #region Scenario Information

    [Serializable]
    class PerceptionCameraData
    {
        public string captureTriggerMode;
        // same as "firstCaptureFrame" of the Perception Camera
        public int startAtFrame;
        public int framesBetweenCaptures;
        public int perceptionCameraIndex = -1;
    }

    [Serializable]
    class LabelerData
    {
        public string name;
        public bool enabled = false;
        public int perceptionCameraIndex = -1;
        public int labelConfigCount = -1;
        public string objectFilter = "";
        public int animationPoseCount = -1;

        public static LabelerData FromLabeler(CameraLabeler labeler)
        {
            if (labeler == null || labeler.enabled == false)
                return null;

            var labelerType = labeler.GetType();
            var labelerName = labelerType.Name;

            if (!PerceptionAnalytics.labelerAllowList.Contains(labelerName))
                return null;

            var labelerData = new LabelerData() {
                name = labelerName,
                enabled = labeler.enabled
            };

            switch (labeler)
            {
                case BoundingBox3DLabeler bb3dl when bb3dl.idLabelConfig != null:
                    labelerData.labelConfigCount = bb3dl.idLabelConfig.labelEntries.Count;
                    break;
                case BoundingBox2DLabeler bb2dl when bb2dl.idLabelConfig != null:
                    labelerData.labelConfigCount = bb2dl.idLabelConfig.labelEntries.Count;
                    break;
                case InstanceSegmentationLabeler isl when isl.idLabelConfig != null:
                    labelerData.labelConfigCount = isl.idLabelConfig.labelEntries.Count;
                    break;
                case KeypointLabeler kpl when kpl.idLabelConfig != null:
                    labelerData.labelConfigCount = kpl.idLabelConfig.labelEntries.Count;
                    labelerData.objectFilter = kpl.objectFilter.ToString();
                    labelerData.animationPoseCount = kpl.animationPoseConfigs?.Count ?? 0;
                    break;
                case ObjectCountLabeler ocl when ocl.labelConfig != null:
                    labelerData.labelConfigCount = ocl.labelConfig.labelEntries.Count;
                    break;
                case SemanticSegmentationLabeler ssl when ssl.labelConfig != null:
                    labelerData.labelConfigCount = ssl.labelConfig.labelEntries.Count;
                    break;
                case RenderedObjectInfoLabeler rol when rol.idLabelConfig != null:
                    labelerData.labelConfigCount = rol.idLabelConfig.labelEntries.Count;
                    break;
                case OcclusionLabeler occl when occl.idLabelConfig != null:
                    labelerData.labelConfigCount = occl.idLabelConfig.labelEntries.Count;
                    break;
            }

            return labelerData;
        }
    }

    [Serializable]
    class SensorData
    {
        public string type;
        public int width;
        public int height;
        public int perceptionCameraIndex = -1;

        public static SensorData FromCameraSensor(CameraSensor sensor)
        {
            if (sensor == null)
                return null;
            var sensorData = new SensorData() {
                type = "Unknown",
                width = sensor.pixelWidth,
                height = sensor.pixelHeight
            };

            if (sensor is UnityCameraSensor _) {
                sensorData.type = "Unity Camera";
            }

            return sensorData;
        }
    }

    [Serializable]
    class MemberData
    {
        public string name;
        public string value;
        public string type;
    }

    [Serializable]
    class ParameterField
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
            return sampler switch
            {
                AnimationCurveSampler _ => new ParameterField() { name = fieldName, distribution = "AnimationCurve" },
                ConstantSampler cs => new ParameterField() { name = fieldName, distribution = "Constant", value = cs.value },
                NormalSampler ns => new ParameterField()
                {
                    name = fieldName,
                    distribution = "Normal",
                    mean = ns.mean,
                    stdDev = ns.standardDeviation,
                    rangeMinimum = ns.range.minimum,
                    rangeMaximum = ns.range.maximum
                },
                UniformSampler us => new ParameterField() { name = fieldName, distribution = "Uniform", rangeMinimum = us.range.minimum, rangeMaximum = us.range.maximum },
                _ => null
            };
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
    class ParameterData
    {
        public string name;
        public string type;
        public List<ParameterField> fields;
    }

    [Serializable]
    class RandomizerData
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
                    var type = memberType.IsSubclassOf(typeof(CategoricalParameterBase))
                        ? "CategoricalParameter" : memberType.Name;
                    var pd = new ParameterData()
                    {
                        name = field.Name,
                        type = type,
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
    class ScenarioCompletedData
    {
        public string platform;
        public PerceptionCameraData[] perceptionCameras;
        public SensorData[] sensors;
        public LabelerData[] labelers;
        public RandomizerData[] randomizers;
        public int framesPerIteration = -1;
        public int iterationsRun = -1;
        public int iterationsPlanned = -1;
        public int totalFrameCount = -1;
        public int startIteration = -1;

        internal static ScenarioCompletedData FromCamerasAndRandomizers(
            PerceptionCamera[] cams,
            ScenarioBase scenario
        )
        {
            var data = new ScenarioCompletedData {
                platform = Application.platform.ToString(),
                perceptionCameras = new PerceptionCameraData[cams.Length]
            };

            // Record data from all Perception cameras
            var allLabelerData = new List<LabelerData>();
            var allPerceptionCameraData = new List<PerceptionCameraData>();
            var allSensorData = new List<SensorData>();

            // for all perception cameras
            for (var i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam == null)
                    continue;

                // Perception Camera Data
                var perceptionCameraData = new PerceptionCameraData()
                {
                    captureTriggerMode = cam.captureTriggerMode.ToString(),
                    startAtFrame = cam.firstCaptureFrame,
                    framesBetweenCaptures = cam.framesBetweenCaptures,
                    perceptionCameraIndex = i
                };
                allPerceptionCameraData.Add(perceptionCameraData);

                // Sensor Data
                var sensorData = SensorData.FromCameraSensor(cam.cameraSensor);
                if (sensorData != null)
                {
                    sensorData.perceptionCameraIndex = i;
                    allSensorData.Add(sensorData);
                }

                // Labeler Data
                foreach (var labeler in cam.labelers)
                {
                    if (labeler == null)
                        continue;

                    var labelerData = LabelerData.FromLabeler(labeler);
                    if (labelerData == null)
                        continue;

                    labelerData.perceptionCameraIndex = i;
                    allLabelerData.Add(labelerData);
                }
            }

            data.perceptionCameras = allPerceptionCameraData.ToArray();
            data.sensors = allSensorData.ToArray();
            data.labelers = allLabelerData.ToArray();

            if (scenario != null)
            {
                data.totalFrameCount = scenario.framesSinceInitialization;
                data.iterationsRun = scenario.currentIteration;
                if (scenario is FixedLengthScenario fls) {
                    data.framesPerIteration = fls.framesPerIteration;
                    data.iterationsPlanned = fls.constants.iterationCount;
                    data.startIteration = fls.constants.startIteration;
                }

                // Randomizers
                var randomizerList = scenario.randomizers.ToArray();
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
            }

            return data;
        }
    }

    #endregion
}
