using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Perception.Content;
using MenuItem = UnityEditor.MenuItem;
using UnityEngine;
using static UnityEngine.Perception.Content.CharacterValidation;
using System.Linq;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.UI;

public class CharacterToolingUI : EditorWindow
{
    static string[] toolbarNames = null;

    CharacterTooling m_contentTests = new CharacterTooling();
    UnityEngine.Object keypointTemplate;

    GameObject selection = null;
    int toolbarSelection = 0;
    bool drawFaceRays = false;
    bool apiResult = false;
    bool checkJoints = false;
    bool vaildCharacter = false;
    string savePath = "Assets/";
    string status = "Unknown";

    void OnSelectionChange()
    {
        selection = Selection.activeGameObject;

        if(selection != null)
        {
            var head = FindBodyPart("head", selection.transform);
            var leftEye = FindBodyPart("leftEye", selection.transform);
            var rightEye = FindBodyPart("rightEye", selection.transform);

            if (head != null || leftEye != null || rightEye != null)
            {
                status = "Character ready to add joints";
                vaildCharacter = true;
                checkJoints = m_contentTests.ValidateNoseAndEars(selection);
            }
            else
            {
                status = "Missing either the head/left or right eye joint transforms!";
                vaildCharacter = false;
            }
        }
    }

    void OnInspectorUpdate()
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

    void OnGUI()
    {
        if (selection != null && selection.GetType() == typeof(GameObject))
        {
            EditorGUILayout.TextField("Selected Character : ", selection.name);
            savePath = EditorGUILayout.TextField("Prefab Save Location : ", savePath);
            GUILayout.Label("Keypoint Template : ", EditorStyles.whiteLargeLabel);
            keypointTemplate = EditorGUILayout.ObjectField(keypointTemplate, typeof(KeypointTemplate), true, GUILayout.MaxWidth(500));

            GUILayout.BeginHorizontal();
            toolbarSelection = GUILayout.Toolbar(toolbarSelection, toolbarNames);
            GUILayout.EndHorizontal();

            switch (toolbarSelection)
            {
                case 0:
                    GUILayout.Label("Character Tools", EditorStyles.whiteLargeLabel);


                    var failedBones = new Dictionary<HumanBone, bool>();
                    var failedPose = new List<GameObject>();
                    GameObject newModel;

                    drawFaceRays = GUILayout.Toggle(drawFaceRays, "Draw Face Rays");
                    GUILayout.Label(string.Format("Create Ears and Nose: {0}", apiResult), EditorStyles.boldLabel);
                    GUILayout.Label(string.Format("Ears and Nose status: {0}", status), EditorStyles.boldLabel);

                    if (checkJoints)
                    {
                        status = "Joints already exist";

                    }
                    else if (!checkJoints && vaildCharacter)
                    {
                        status = "Joints don't exist";
                        if (GUILayout.Button("Create Nose and Ears", GUILayout.Width(160)))
                        {
                            if (savePath == "Assets/")
                                apiResult = m_contentTests.CharacterCreateNose(selection, out newModel, keypointTemplate, drawFaceRays);
                            else
                                apiResult = m_contentTests.CharacterCreateNose(selection, out newModel, keypointTemplate, drawFaceRays, savePath);

                            var modelValidate = m_contentTests.ValidateNoseAndEars(newModel);

                            if (modelValidate)
                                status = "Ear and Nose joints created";
                            else if (!modelValidate)
                                status = "Failed to create the Ear and Nose joints";
                        }
                    }

                    break;

                case 1:

                    GUILayout.Label("Character Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation for Character : {0}", apiResult), EditorStyles.whiteLabel);

                    var animator = selection.GetComponentInChildren<Animator>();

                    if (animator != null)
                    {
                        GUILayout.Label(string.Format("Character is Human: {0}", animator.avatar.isHuman), EditorStyles.boldLabel);
                        GUILayout.Label(string.Format("Character is Valid: {0}", animator.avatar.isValid), EditorStyles.boldLabel);
                    }

                    if (GUILayout.Button("Validate Bones", GUILayout.Width(160)))
                    {
                        apiResult = m_contentTests.CharacterRequiredBones(selection, out failedBones);

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
                            GUILayout.Label(string.Format("Required Bones Present : {0}", apiResult), EditorStyles.whiteLabel);
                        }

                    }

                    if (GUILayout.Button("Validate Pose Data", GUILayout.Width(160)))
                    {
                        apiResult = m_contentTests.CharacterPoseData(selection, out failedPose);
                    }

                    break;
            }
        }
        else
        {
            GUILayout.Label("The selected assets is invalid, please select a different Game Object.", EditorStyles.boldLabel);
        }
    }
}

