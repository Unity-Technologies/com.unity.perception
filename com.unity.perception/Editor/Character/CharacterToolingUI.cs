using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Perception.Content;
using MenuItem = UnityEditor.MenuItem;
using UnityEngine;
using System.Linq;
using UnityEngine.Perception.GroundTruth;

public class CharacterToolingUI : EditorWindow
{
    static string[] s_ToolbarNames = null;

    CharacterTooling m_ContentTests = new CharacterTooling();
    Object m_KeypointTemplate;

    GameObject m_Selection = null;
    int m_ToolbarSelection = 0;
    bool m_DrawFaceRays = false;
    bool m_ApiResult = false;
    bool m_CheckJoints = false;
    bool m_VaildCharacter = false;
    string m_SavePath = "Assets/";
    string m_Status = "Unknown";

    void OnSelectionChange()
    {
        m_Selection = Selection.activeGameObject;

        if(m_Selection != null)
        {
            var head = CharacterValidation.FindBodyPart(m_Selection, HumanBodyBones.Head);
            var leftEye = CharacterValidation.FindBodyPart(m_Selection, HumanBodyBones.LeftEye);
            var rightEye = CharacterValidation.FindBodyPart(m_Selection, HumanBodyBones.RightEye);

            if (head != m_Selection.transform || leftEye != m_Selection.transform || rightEye != m_Selection.transform)
            {
                m_Status = "Character ready to add joints";
                m_VaildCharacter = true;
                m_CheckJoints = m_ContentTests.ValidateNoseAndEars(m_Selection);
            }
            else
            {
                m_Status = "Missing either the head/left or right eye joint transforms!";
                m_VaildCharacter = false;
            }
        }
    }

    void OnInspectorUpdate()
    {
        Repaint();
        m_Selection = Selection.activeGameObject;
    }

    [MenuItem("Window/Perception Character Tool")]
    static void Init()
    {
        s_ToolbarNames = new string[] { "Keypoints", "Validation" };
        CharacterToolingUI window = (CharacterToolingUI)GetWindow(typeof(CharacterToolingUI));
        window.autoRepaintOnSceneChange = true;
        window.Show();
    }

    void OnGUI()
    {
        if (m_Selection != null && m_Selection.GetType() == typeof(GameObject))
        {
            EditorGUILayout.TextField("Selected GameObject : ", m_Selection.name);
            m_SavePath = EditorGUILayout.TextField("Prefab Save Location : ", m_SavePath);
            GUILayout.Label("Keypoint Template : ", EditorStyles.whiteLargeLabel);
            m_KeypointTemplate = EditorGUILayout.ObjectField(m_KeypointTemplate, typeof(KeypointTemplate), true, GUILayout.MaxWidth(500));

            GUILayout.BeginHorizontal();
            m_ToolbarSelection = GUILayout.Toolbar(m_ToolbarSelection, s_ToolbarNames);
            GUILayout.EndHorizontal();

            switch (m_ToolbarSelection)
            {
                case 0:
                    GUILayout.Label("Character Tools", EditorStyles.whiteLargeLabel);


                    var failedBones = new Dictionary<HumanBone, bool>();
                    var failedPose = new List<GameObject>();
                    GameObject newModel;

                    m_DrawFaceRays = GUILayout.Toggle(m_DrawFaceRays, "Draw Face Rays");
                    GUILayout.Label(string.Format("Create Ears and Nose: {0}", m_ApiResult), EditorStyles.boldLabel);
                    GUILayout.Label(string.Format("Ears and Nose status: {0}", m_Status), EditorStyles.boldLabel);

                    if (m_CheckJoints)
                    {
                        m_Status = "Joints already exist";

                    }
                    else if (!m_CheckJoints && m_VaildCharacter)
                    {
                        m_Status = "Joints don't exist";
                        if (GUILayout.Button("Create Nose and Ears", GUILayout.Width(160)))
                        {
                            if (m_SavePath == "Assets/")
                                m_ApiResult = m_ContentTests.CharacterCreateNose(m_Selection, out newModel, m_KeypointTemplate, m_DrawFaceRays);
                            else
                                m_ApiResult = m_ContentTests.CharacterCreateNose(m_Selection, out newModel, m_KeypointTemplate, m_DrawFaceRays, m_SavePath);

                            var modelValidate = m_ContentTests.ValidateNoseAndEars(newModel);

                            if (modelValidate)
                                m_Status = "Ear and Nose joints created";
                            else if (!modelValidate)
                                m_Status = "Failed to create the Ear and Nose joints";
                        }
                    }

                    break;

                case 1:

                    GUILayout.Label("Character Validation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label(string.Format("Validation for Character : {0}", m_ApiResult), EditorStyles.whiteLabel);

                    var animator = m_Selection.GetComponentInChildren<Animator>();

                    if (animator != null)
                    {
                        GUILayout.Label(string.Format("Character is Human: {0}", animator.avatar.isHuman), EditorStyles.boldLabel);
                        GUILayout.Label(string.Format("Character is Valid: {0}", animator.avatar.isValid), EditorStyles.boldLabel);
                    }

                    if (GUILayout.Button("Validate Bones", GUILayout.Width(160)))
                    {
                        m_ApiResult = m_ContentTests.CharacterRequiredBones(m_Selection, out failedBones);

                        if (failedBones.Count > 0)
                        {
                            for (int i = 0; i < CharacterValidation.s_RequiredBones.Length; i++)
                            {
                                for (int b = 0; b < failedBones.Count; b++)
                                {
                                    var bone = failedBones.ElementAt(i);
                                    var boneKey = bone.Key;
                                    var boneValue = bone.Value;

                                    if (CharacterValidation.s_RequiredBones[i] == boneKey.humanName)
                                    {
                                        GUILayout.Label(string.Format("Bone {0}: {1}", CharacterValidation.s_RequiredBones[i], "Missing"), EditorStyles.boldLabel);
                                    }
                                }
                            }
                        }
                        else if (failedBones.Count == 0)
                        {
                            GUILayout.Label(string.Format("Required Bones Present : {0}", m_ApiResult), EditorStyles.whiteLabel);
                        }

                    }

                    if (GUILayout.Button("Validate Pose Data", GUILayout.Width(160)))
                    {
                        m_ApiResult = m_ContentTests.CharacterPoseData(m_Selection, out failedPose);
                    }

                    break;
            }
        }
        else
        {
            GUILayout.Label("The selected asset(s) is invalid, please select a Game Object.", EditorStyles.boldLabel);
        }
    }
}

