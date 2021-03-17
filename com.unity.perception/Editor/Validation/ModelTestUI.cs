using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Perception.Content;
using MenuItem = UnityEditor.MenuItem;
using UnityEngine;
using static UnityEngine.Perception.Content.CharacterValidation;
using System.Linq;

public class AssetValidation : EditorWindow
{
    private static string[] toolbarNames = null;
    private enum TestResults
    {
        Inconclusive,
        Pass,
        Fail,
        Running
    }

    private TestResults testResults = new TestResults();
    private ContentValidation contentTests = new ContentValidation();

    private int toolbarSelection = 0;

    private GameObject selection = null;
    private MeshRenderer[] meshRenderers = null;
    private MeshFilter[] meshFilters = null;
    private Animator animator = null;
    private SkinnedMeshRenderer skinnedMeshRenderer = null;
    private List<string> failData = new List<string>();

    private bool drawFaceRays = false;
    private bool drawIssueEdges = false;

    private void OnSelectionChange()
    {
        UpdateSelection();
    }

    private void OnInspectorUpdate()
    {
        UpdateSelection();
    }

    [MenuItem("Window/Content Validation")]
    static void Init()
    {
        toolbarNames = new string[] { "Assets", "UV", "Mesh", "Character", "Data" };
        AssetValidation window = (AssetValidation)GetWindow(typeof(AssetValidation));
        window.autoRepaintOnSceneChange = true;
        window.Show();
    }

    private void OnGUI()
    {
        if (selection != null)
        {
            EditorGUILayout.TextField("Selected Asset : ", selection.name);

            GUILayout.BeginHorizontal();
            toolbarSelection = GUILayout.Toolbar(toolbarSelection, toolbarNames);
            GUILayout.EndHorizontal();

            switch (toolbarSelection)
            {
                case 0:
                    GUILayout.Label("Asset Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation Result : {0}", testResults), EditorStyles.boldLabel);

                    if (GUILayout.Button("Validate Scale", GUILayout.Width(130)))
                    {
                        testResults = TestResults.Running;
                        var scaleResult = contentTests.TestScale(meshRenderers);

                        if (scaleResult)
                            testResults = TestResults.Pass;
                        else if (!scaleResult)
                            testResults = TestResults.Fail;
                    }

                    GUILayout.Label("Empty Game Objects Validation", EditorStyles.boldLabel);
                    if (GUILayout.Button("Validate", GUILayout.Width(130)))
                    {
                        testResults = TestResults.Running;
                        var result = contentTests.EmptyComponentsTest(selection);
                        if (result)
                            testResults = TestResults.Pass;
                        if (!result)
                            testResults = TestResults.Fail;
                    }

                    GUILayout.Label("Transform Validation", EditorStyles.boldLabel);
                    if (GUILayout.Button("Validate Transform(s)", GUILayout.Width(140)))
                    {
                        testResults = TestResults.Running;
                        var failedObjects = new List<GameObject>();
                        var transformTest = contentTests.TransformTest(selection, out failedObjects);
                        if (transformTest && failedObjects.Count == 0)
                            testResults = TestResults.Pass;
                        else if (!transformTest || failedObjects.Count > 0)
                            testResults = TestResults.Fail;
                    }
                    break;

                case 1:
                    GUILayout.Label("UV Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation for UV : {0}", testResults), EditorStyles.boldLabel);

                    if (GUILayout.Button("Validate UV Data", GUILayout.Width(130)))
                    {
                        testResults = TestResults.Running;
                        var uvDataTest = contentTests.UVDataTest(meshFilters);
                        testResults = TestResults.Running;
                        if (uvDataTest)
                            testResults = TestResults.Pass;
                        if (!uvDataTest)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate UV Range", GUILayout.Width(130)))
                    {
                        testResults = TestResults.Running;
                        var uvInvertedTest = contentTests.UVRangeTest(meshFilters);
                        if (uvInvertedTest)
                            testResults = TestResults.Pass;
                        if (!uvInvertedTest)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate UV Positive", GUILayout.Width(130)))
                    {
                        testResults = TestResults.Running;
                        var uvPositiveTest = contentTests.UVPositiveTest(meshFilters);

                        if (uvPositiveTest)
                            testResults = TestResults.Pass;
                        if (!uvPositiveTest)
                            testResults = TestResults.Fail;
                    }

                    break;

                case 2:
                    GUILayout.Label("Mesh Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation for Mesh : {0}", testResults), EditorStyles.boldLabel);

                    drawFaceRays = GUILayout.Toggle(drawFaceRays, "Draw Asset Mesh");
                    drawIssueEdges = GUILayout.Toggle(drawIssueEdges, "Draw Issue Edges");

                    var failedMeshes = new List<string>();
                    var failedVerts = new List<Mesh>();

                    if (GUILayout.Button("Validate Mesh Normals", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        var invertedNormals = contentTests.MeshInvertedNormalsTest(meshFilters, out failedMeshes);
                        if (invertedNormals)
                            testResults = TestResults.Pass;
                        if (!invertedNormals)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate Closed Mesh", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        var test = contentTests.MeshOpenFaceTest(meshFilters, out failedMeshes, drawFaceRays, drawIssueEdges);
                        if (test == true)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate Vertices's", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        var test = contentTests.MeshDetectUnWeldedVerts(meshFilters, out failedMeshes);

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate for Spikes", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        var test = contentTests.MeshDetectSpikes(meshFilters, drawIssueEdges);

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate Texel Density", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        var failed = new List<GameObject>();
                        
                        var test = contentTests.AssetTexelDensity(meshFilters, meshRenderers, out failed);

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate Pivot Point", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        var failed = new List<GameObject>();
                        var test = contentTests.AssetCheckPivotPoint(selection, out failed);

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    break;

                case 3:
                    GUILayout.Label("Character Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation for Character : {0}", testResults), EditorStyles.whiteLabel);

                    if (animator != null)
                    {
                        GUILayout.Label(string.Format("Character is Human: {0}", animator.avatar.isHuman), EditorStyles.boldLabel);
                        GUILayout.Label(string.Format("Character is Valid: {0}", animator.avatar.isValid), EditorStyles.boldLabel);
                    }

                    drawFaceRays = GUILayout.Toggle(drawFaceRays, "Draw Face Rays");

                    var failedBones = new Dictionary<HumanBone, bool>();
                    var failedPose = new List<GameObject>();
                                       
                    if (GUILayout.Button("Validate Bones", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        
                        var test = contentTests.CharacterRequiredBones(animator, out failedBones);

                        if (failedBones.Count > 0)
                        {
                            for (int i = 0; i < RequiredBones.Length; i++)
                            {
                                for (int b = 0; b < failedBones.Count; b++)
                                {
                                    var bone = failedBones.ElementAt(i);
                                    var boneKey = bone.Key;
                                    var boneValue = bone.Value;

                                    if (RequiredBones[i] == boneKey.humanName)
                                    {
                                        GUILayout.Label(string.Format("Bone {0}: {1}", RequiredBones[i], "Missing"), EditorStyles.boldLabel);
                                    }
                                }
                            }
                        }
                        else if (failedBones.Count == 0)
                        {
                            GUILayout.Label(string.Format("Required Bones Present : {0}", TestResults.Pass), EditorStyles.whiteLabel);
                        }

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate Pose Data", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;
                        
                        var test = contentTests.CharacterPoseData(selection, out failedPose);

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    if (GUILayout.Button("Create Nose and Ears", GUILayout.Width(160)))
                    {
                        testResults = TestResults.Running;

                        var test = contentTests.CharacterCreateNose(selection, animator, skinnedMeshRenderer, drawFaceRays);
                        

                        if (test)
                            testResults = TestResults.Pass;
                        if (!test)
                            testResults = TestResults.Fail;
                    }

                    break;

                case 4:
                    GUILayout.Label("Asset Results", EditorStyles.boldLabel);

                    break;
            }
        }
    }

    private void UpdateSelection()
    {
        selection = Selection.activeGameObject;
        if (selection != null)
        {
            meshRenderers = selection.GetComponentsInChildren<MeshRenderer>();
            meshFilters = selection.GetComponentsInChildren<MeshFilter>();
            animator = selection.GetComponentInChildren<Animator>();
            skinnedMeshRenderer = selection.GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }
}

