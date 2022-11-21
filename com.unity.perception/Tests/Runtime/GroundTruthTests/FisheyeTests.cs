namespace GroundTruthTests
{
#if PERCEPTION_EXPERIMENTAL
    [TestFixture]
    public class FisheyeTests
    {
        List<Object> m_ObjectsToDestroy = new();

        [UnitySetUp]
        public virtual IEnumerator SetUp()
        {
            DatasetCapture.OverrideEndpoint(new NoOutputEndpoint());
            DatasetCapture.ResetSimulation();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in m_ObjectsToDestroy)
                Object.DestroyImmediate(o);

            m_ObjectsToDestroy.Clear();

            DatasetCapture.ResetSimulation();

            Time.timeScale = 1;
        }

        static IEnumerable<(float fov, float objectAngle, bool shouldSeeObject)> FieldOfViewTestCases()
        {
            yield return (45, 0, true);
            yield return (45, 45, false);
            yield return (45, 90, false);
            yield return (45, 180, false);
            yield return (90, 0, true);
            yield return (90, 45, true);
            yield return (90, 90, false);
            yield return (90, 180, false);
            yield return (180, 0, true);
            yield return (180, 45, true);
            yield return (180, 90, true);
            yield return (180, 180, false);
            yield return (360, 0, true);
            yield return (360, 45, true);
            yield return (360, 90, true);
            yield return (360, 180, true);
        }

        [UnityTest]
        public IEnumerator FieldOfViewTest(
            [ValueSource(nameof(FieldOfViewTestCases))](float fov, float objectAngle, bool shouldSeeObject) args)
        {
            // Create a fisheye camera at the origin of the scene.
            var camera = SetupFisheyeCamera(args.fov);

            // Create a labeled quad and offset it from the origin 2 meters forward to place it in view of the camera.
            var quad = CreateLabeledQuad();
            var quadTransform = quad.transform;
            quadTransform.position = new Vector3(0, 0, 2);

            // Rotate the quad out of view by the given objectAngle arg.
            var rotationOrigin = new GameObject("Labeled Quad Rotation Origin");
            var rotationOriginTransform = rotationOrigin.transform;
            quadTransform.parent = rotationOriginTransform;
            rotationOriginTransform.rotation = Quaternion.Euler(0, args.objectAngle, 0);
            AddTestObjectForCleanup(rotationOrigin);

            // Check to see if the quad was spotted in the captured frame of the fisheye camera.
            camera.EnableChannel<InstanceIdChannel>();
            camera.RenderedObjectInfosCalculated += (_, infos, _) =>
            {
                Assert.AreEqual(infos.Length, args.shouldSeeObject ? 1 : 0);
            };

            yield return null;
        }

        PerceptionCamera SetupFisheyeCamera(float fov = 90f)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.SetActive(false);
            cameraObject.transform.position = new Vector3(0, 0, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            var targetTexture = new RenderTexture(512, 512, 32, GraphicsFormat.R8G8B8A8_SRGB);
            camera.targetTexture = targetTexture;
#if HDRP_PRESENT
            cameraObject.AddComponent<HDAdditionalCameraData>();
#endif

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.showVisualizations = false;
            perceptionCamera.cameraSensor = new CircularFisheyeCameraSensor
            {
                cubemapResolution = CubemapResolution._512x512,
                fieldOfView = fov
            };

            AddTestObjectForCleanup(cameraObject);
            cameraObject.SetActive(true);
            return perceptionCamera;
        }

        Labeling CreateLabeledQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "LabeledQuad";
            var labeling = quad.AddComponent<Labeling>();
            labeling.labels.Add("label");
            AddTestObjectForCleanup(quad);
            return labeling;
        }

        void AddTestObjectForCleanup(Object obj) => m_ObjectsToDestroy.Add(obj);
    }
#endif
}
