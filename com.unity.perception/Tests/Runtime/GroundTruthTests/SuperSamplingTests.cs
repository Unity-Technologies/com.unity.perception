using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GroundTruthTests
{
    [TestFixture]
    public class SuperSamplingTests : GroundTruthTestBase
    {
        const int k_TargetTextureWidth = 32;

        [UnityTest]
        public IEnumerator SuperSamplingProducesValidOutput(
            [Values(
                SuperSamplingFactor.None,
                SuperSamplingFactor._2X,
                SuperSamplingFactor._4X,
                SuperSamplingFactor._8X)]
            SuperSamplingFactor scaleFactor)
        {
            var perceptionCamera = SetupPerceptionCamera(scaleFactor);

            // Create a red quad and place it in front of the camera.
            var quad = CreateLabeledQuad();
            quad.name = "TopRedQuad";
            quad.transform.position = Vector3.forward;
            TestHelper.SetColor(quad, new Color32(255, 0, 0, 255));

            // Rotate the quad to force aliasing to occur in the rendered image
            quad.transform.rotation = Quaternion.Euler(0, 0, 45);

            // Readback the RGB channel's output texture.
            var halfOutputWidth = k_TargetTextureWidth / 2;
            var rgbChannel = perceptionCamera.EnableChannel<RGBChannel>();
            rgbChannel.outputTextureReadback += (_, pixelData) =>
            {
                var cornerColor = pixelData[0];
                var centerColor = pixelData[k_TargetTextureWidth * halfOutputWidth + halfOutputWidth];

                var colorHash = new HashSet<Color32>();
                for (var i = 0; i < pixelData.Length; i++)
                    colorHash.Add(pixelData[i]);

                Assert.AreEqual(new Color32(0, 0, 255, 255), cornerColor);
                Assert.AreEqual(new Color32(255, 0, 0, 255), centerColor);

                if (scaleFactor == SuperSamplingFactor.None)
                {
                    // When super resolution is disabled, there should only be red or blue pixels in the image.
                    Assert.AreEqual(2, colorHash.Count);
                }
                else
                {
                    // When super resolution is enabled, there should be blended pixels in the image around the
                    // edges of the rendered quad geometry.
                    Assert.Greater(colorHash.Count, 2);
                }
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator SuperSamplingProducesValidBoundingBoxes([Values(
            SuperSamplingFactor.None,
            SuperSamplingFactor._2X,
            SuperSamplingFactor._4X,
            SuperSamplingFactor._8X)] SuperSamplingFactor scaleFactor)
        {
            var perceptionCamera = SetupPerceptionCamera(scaleFactor);

            // Create a quad and place it in front of the camera.
            var quad = CreateLabeledQuad();
            quad.transform.position = Vector3.forward;

            // Confirm that one bounding box is output for the quad.
            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += (_, boundingBoxes, _) =>
            {
                Assert.AreEqual(1, boundingBoxes.Length);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator SuperSamplingProducesValidDepthChannel([Values(
            SuperSamplingFactor.None,
            SuperSamplingFactor._2X,
            SuperSamplingFactor._4X,
            SuperSamplingFactor._8X)] SuperSamplingFactor scaleFactor)
        {
            var perceptionCamera = SetupPerceptionCamera(scaleFactor);

            // Create a quad and place it in front of the camera.
            var quad = CreateLabeledQuad();
            quad.transform.position = Vector3.forward;

            // Readback the depth channel's output texture.
            var channel = perceptionCamera.EnableChannel<DepthChannel>();
            channel.outputTextureReadback += (_, pixelDepthValues) =>
            {
                Assert.AreEqual(k_TargetTextureWidth * k_TargetTextureWidth, pixelDepthValues.Length);

                // Confirm that there are only two unique depth values present in the depth image:
                // the quad's depth and the skybox's depth.
                var uniqueDepthValues = new HashSet<float4>();
                foreach (var depthValue in pixelDepthValues)
                    uniqueDepthValues.Add(depthValue);
                Assert.AreEqual(2, uniqueDepthValues.Count);

                Assert.IsTrue(uniqueDepthValues.Contains(new float4(1f, 0f, 0f, 1f)));
                Assert.IsTrue(uniqueDepthValues.Contains(new float4(0f, 0f, 0f, 0f)));
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator SuperSamplingProducesValidKeypoints([Values(
            SuperSamplingFactor.None,
            SuperSamplingFactor._2X,
            SuperSamplingFactor._4X,
            SuperSamplingFactor._8X)] SuperSamplingFactor scaleFactor)
        {
            var perceptionCamera = SetupPerceptionCamera(scaleFactor);

            // Create keypoint config and template.
            var idLabelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
            idLabelConfig.Init(new List<IdLabelEntry> { new() { id = 1, label = "label" } });
            var quadKeypointTemplate = CreateQuadKeypointTemplate();

            // Create a quad and place it in front of the camera.
            var quad = CreateLabeledQuad();
            quad.transform.position = Vector3.forward;

            // Setup the quad's keypoints.
            SetupQuadKeypointJoints(quad);

            // Add a keypoint labeler to the perception camera.
            var keypointLabeler = new KeypointLabeler(idLabelConfig, quadKeypointTemplate);
            perceptionCamera.AddLabeler(keypointLabeler);

            // Confirm that the 4 corner keypoints of the quad are reported by the keypoint labeler.
            keypointLabeler.KeypointsComputed += (_, setsOfKeypoints) =>
            {
                Assert.AreEqual(1, setsOfKeypoints.Count);

                var keypoints = setsOfKeypoints[0].keypoints;
                Assert.AreEqual(4, keypoints.Length);

                // Confirm that all of the keypoints have a state of 2,
                // meaning that they're all visible and not occluded.
                CollectionAssert.AreEqual(Enumerable.Repeat(2, 4), keypoints.Select(k => k.state));
            };

            yield return null;
        }

        PerceptionCamera SetupPerceptionCamera(SuperSamplingFactor scaleFactor)
        {
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.SetActive(false);

            camera.clearFlags = CameraClearFlags.Color;
            camera.backgroundColor = Color.blue;
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2f;
            camera.targetTexture =
                new RenderTexture(k_TargetTextureWidth, k_TargetTextureWidth, 16, GraphicsFormat.R8G8B8A8_UNorm);

#if HDRP_PRESENT
            if (!cameraObject.TryGetComponent<HDAdditionalCameraData>(out var cameraData))
                cameraData = cameraObject.AddComponent<HDAdditionalCameraData>();
            cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            cameraData.backgroundColorHDR = Color.blue;
#endif

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            var cameraSensor = (UnityCameraSensor)perceptionCamera.cameraSensor;
            cameraSensor.superSamplingFactor = scaleFactor;

            cameraObject.SetActive(true);
            AddTestObjectForCleanup(cameraObject);
            return perceptionCamera;
        }

        GameObject CreateLabeledQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "quad";
            var labeling = quad.AddComponent<Labeling>();
            labeling.labels.Add("label");
            AddTestObjectForCleanup(quad);
            return quad;
        }

        static KeypointTemplate CreateQuadKeypointTemplate()
        {
            const float selfOcclusionDistance = 0.01f;
            var keypoints = new KeypointDefinition[]
            {
                new()
                {
                    label = "LowerLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new()
                {
                    label = "UpperLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new()
                {
                    label = "UpperRight",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new()
                {
                    label = "LowerRight",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                }
            };

            var template = ScriptableObject.CreateInstance<KeypointTemplate>();
            template.templateID = Guid.NewGuid().ToString();
            template.templateName = "QuadKeypointTemplate";
            template.jointTexture = null;
            template.skeletonTexture = null;
            template.keypoints = keypoints;
            template.skeleton = Array.Empty<SkeletonDefinition>();;

            return template;
        }

        static void SetupQuadJoint(GameObject cube, string label, float x, float y, float z, float? selfOcclusionDistance = null)
        {
            var joint = new GameObject($"Joint-{label}");
            joint.transform.SetParent(cube.transform, false);
            joint.transform.localPosition = new Vector3(x, y, z);
            var jointLabel = joint.AddComponent<JointLabel>();
            jointLabel.labels.Add(label);
            if (selfOcclusionDistance.HasValue)
            {
                jointLabel.overrideSelfOcclusionDistance = true;
                jointLabel.selfOcclusionDistance = selfOcclusionDistance.Value;
            }
            else
                jointLabel.overrideSelfOcclusionDistance = false;
        }

        static void SetupQuadKeypointJoints(GameObject cube, float? selfOcclusionDistance = null)
        {
            const float dim = 0.5f;
            SetupQuadJoint(cube, "LowerLeft", -dim, -dim, -dim, selfOcclusionDistance);
            SetupQuadJoint(cube, "UpperLeft", -dim, dim, -dim, selfOcclusionDistance);
            SetupQuadJoint(cube, "UpperRight", dim, dim, -dim, selfOcclusionDistance);
            SetupQuadJoint(cube, "LowerRight", dim, -dim, -dim, selfOcclusionDistance);
        }
    }
}
