using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    class SoloMessageBuilder : JsonMessageBuilder
    {
        protected SoloEndpoint solo { get; private set; }

        public SoloMessageBuilder(SoloEndpoint solo)
        {
            this.solo = solo;
        }

        JArray ToJArray(Vector3 vector3)
        {
            return new JArray { vector3.x, vector3.y, vector3.z };
        }

        public override void AddTensor(string key, Tensor tensor)
        {
            switch (key)
            {
                case "dimension":
                    var vec2 = TensorBuilder.ToVector2(tensor);
                    currentJToken[key] = new JArray { vec2.x, vec2.y };
                    break;
                case "position":
                case "rotation":
                case "velocity":
                case "acceleration":
                    var vec3 = TensorBuilder.ToVector3(tensor);
                    currentJToken[key] = new JArray { vec3.x, vec3.y, vec3.z };
                    break;
                case "matrix":
                    var matrix = TensorBuilder.ToFloat3X3(tensor);
                    currentJToken[key] = new JArray
                    {
                        matrix.c0.x, matrix.c0.y, matrix.c0.z,
                        matrix.c1.x, matrix.c1.y, matrix.c1.z,
                        matrix.c2.x, matrix.c2.y, matrix.c2.z,
                    };
                    break;
                default:
                    currentJToken[key] = new JArray(tensor.buffer);
                    break;
            }
        }

        public override IMessageBuilder AddNestedMessage(string key)
        {
            var nested = new SoloMessageBuilder(solo);

            if (nestedValue.ContainsKey(key))
            {
                Debug.LogWarning($"Report data with key [{key}] will be overridden by new values");
            }

            nestedValue[key] = nested;
            return nested;
        }

        public override IMessageBuilder AddNestedMessageToVector(string arraykey)
        {
            if (!nestedArrays.TryGetValue(arraykey, out var nestedList))
            {
                nestedList = new List<JsonMessageBuilder>();
                nestedArrays[arraykey] = nestedList;
            }
            var nested = new SoloMessageBuilder(solo);
            nestedList.Add(nested);
            return nested;
        }
    }
    class SoloFrameMessageBuilder : SoloMessageBuilder
    {
        Frame m_Frame;

        public SoloFrameMessageBuilder(SoloEndpoint solo, Frame frame) : base(solo)
        {
            m_Frame = frame;
        }

        public override void AddEncodedImage(string key, string extension, byte[] value)
        {
            string filename = null;

            // If perception camera is set to not save rgb files to disk, we'll just report
            // null for the filename
            if (value.Length > 0)
            {
                filename = $"step{m_Frame.step}.{key}.{extension.ToLower()}";

                // write out the file
                var path = solo.GetSequenceDirectoryPath(m_Frame);
                path = PathUtils.CombineUniversal(path, filename);

                PathUtils.WriteAndReportImageFile(path, value);
                solo.RegisterFile(path);
            }

            // Add the filename to the json
            currentJToken["filename"] = filename;
        }

        public override IMessageBuilder AddNestedMessage(string key)
        {
            var nested = new SoloFrameMessageBuilder(solo, m_Frame);

            if (nestedValue.ContainsKey(key))
            {
                Debug.LogWarning($"Report data with key [{key}] will be overridden by new values");
            }

            nestedValue[key] = nested;
            return nested;
        }

        public override IMessageBuilder AddNestedMessageToVector(string arraykey)
        {
            if (!nestedArrays.TryGetValue(arraykey, out var nestedList))
            {
                nestedList = new List<JsonMessageBuilder>();
                nestedArrays[arraykey] = nestedList;
            }
            var nested = new SoloFrameMessageBuilder(solo, m_Frame);
            nestedList.Add(nested);
            return nested;
        }

        public static bool ValidateAllFilesAreWrittenForFrame(string sequenceFolder, string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                return false;
            }

            JToken frameConfig;
            try
            {
                frameConfig = JToken.Parse(configJson);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Debug.Log($"Can't parse config json {configJson} with error {e}");
                return false;
            }

            var captures = frameConfig["captures"];
            if (captures == null)
            {
                Debug.Log($"There were no captures for frame in this folder: {sequenceFolder}");
                return true;
            }

            bool CheckIfMessageFileExists(JToken soloMessageRecord)
            {
                if (soloMessageRecord != null && soloMessageRecord["filename"] != null)
                {
                    var framePath = PathUtils.CombineUniversal(sequenceFolder, soloMessageRecord["filename"].ToString());
                    if (!System.IO.File.Exists(framePath))
                    {
                        Debug.Log($"there is no file {framePath}");
                        return false;
                    }

                    if (new System.IO.FileInfo(framePath).Length == 0)
                    {
                        Debug.Log($"file is empty {framePath}");
                        return false;
                    }
                }

                return true;
            }

            foreach (var capture in captures)
            {
                //png frame from camera
                var cameraOutputIsOk = CheckIfMessageFileExists(capture);
                if (!cameraOutputIsOk)
                {
                    return false;
                }

                // png frame per each included annotation
                if (capture["annotations"] is JArray annotations)
                {
                    foreach (var annotation in annotations)
                    {
                        // check if annotation generates a frame
                        var annotationFilesIsOk = CheckIfMessageFileExists(annotation);
                        if (!annotationFilesIsOk)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
