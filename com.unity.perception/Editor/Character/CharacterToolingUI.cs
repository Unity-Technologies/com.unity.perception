using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Perception.Content;
using MenuItem = UnityEditor.MenuItem;
using UnityEngine;
using static UnityEngine.Perception.Content.CharacterValidation;
using System.Linq;

public class CharacterToolingUI : EditorWindow
{
    private static string[] toolbarNames = null;
    private enum TestResults
    {
        Inconclusive,
        Pass,
        Fail,
        Running
    }

    TestResults m_testResults = new TestResults();
    CharacterTooling m_contentTests = new CharacterTooling();

    GameObject selection = null;
    int toolbarSelection = 0;
    bool drawFaceRays = false;
    string savePath = string.Empty;

    private void OnSelectionChange()
    {
        selection = Selection.activeGameObject;
    }

    private void OnInspectorUpdate()
    {
        Repaint();
        selection = Selection.activeGameObject;
    }

    [MenuItem("Window/Perception Character Tool")]
    static void Init()
    {
        toolbarNames = new string[] { "Keypoints", "Validation" };
        CharacterToolingUI window = (CharacterToolingUI)GetWindow(typeof(CharacterToolingUI));
        window.autoRepaintOnSceneChange = true;
        window.Show();
    }

    private void OnGUI()
    {
        if (selection != null && selection.GetType() == typeof(GameObject))
        {
            EditorGUILayout.TextField("Selected Asset : ", selection.name);
            savePath = EditorGUILayout.TextField("Prefab Save Location : ", savePath);

            GUILayout.BeginHorizontal();
            toolbarSelection = GUILayout.Toolbar(toolbarSelection, toolbarNames);
            GUILayout.EndHorizontal();

            switch (toolbarSelection)
            {
                case 0:
                    GUILayout.Label("Character Tools", EditorStyles.whiteLargeLabel);
                    var test = true;
                    var status = "Unknown";
                    var checkForJoints = m_contentTests.ValidateNoseAndEars(selection);
                    var failedBones = new Dictionary<HumanBone, bool>();
                    var failedPose = new List<GameObject>();

                    drawFaceRays = GUILayout.Toggle(drawFaceRays, "Draw Face Rays");
                    GUILayout.Label(string.Format("Create Ears and Nose: {0}", test), EditorStyles.boldLabel);
                    GUILayout.Label(string.Format("Ears and Nose status: {0}", status), EditorStyles.boldLabel);


                    if (GUILayout.Button("Create Nose and Ears", GUILayout.Width(160)))
                    {
                        m_testResults = TestResults.Running;
                      
                        if (!checkForJoints)
                        {
                            if (savePath == string.Empty)
                                test = m_contentTests.CharacterCreateNose(selection, drawFaceRays);
                            else
                                test = m_contentTests.CharacterCreateNose(selection, drawFaceRays, savePath);

                            m_testResults = test ? TestResults.Fail : TestResults.Pass;

                            if (test)
                                status = "Ear and Nose Joints have been created on the Asset";
                            else if (!test)
                                status = "Failed to create the Ear and Nose Joints";
                        }
                        else if(checkForJoints)
                        {
                            status = "Joints have already been created on this Asset";
                        }
                        Repaint();
                    }

                    break;

                case 1:

                    GUILayout.Label("Character Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation for Character : {0}", m_testResults), EditorStyles.whiteLabel);

                    var animator = selection.GetComponentInChildren<Animator>();

                    if (animator != null)
                    {
                        GUILayout.Label(string.Format("Character is Human: {0}", animator.avatar.isHuman), EditorStyles.boldLabel);
                        GUILayout.Label(string.Format("Character is Valid: {0}", animator.avatar.isValid), EditorStyles.boldLabel);
                    }

                    if (GUILayout.Button("Validate Bones", GUILayout.Width(160)))
                    {
                        m_testResults = TestResults.Running;

                        test = m_contentTests.CharacterRequiredBones(selection, out failedBones);

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

                        m_testResults = test ? TestResults.Pass : TestResults.Fail;
                    }

                    if (GUILayout.Button("Validate Pose Data", GUILayout.Width(160)))
                    {
                        m_testResults = TestResults.Running;

                        test = m_contentTests.CharacterPoseData(selection, out failedPose);

                        m_testResults = test ? TestResults.Pass : TestResults.Fail;
                    }

                    break;
            }
        }
    }
}

