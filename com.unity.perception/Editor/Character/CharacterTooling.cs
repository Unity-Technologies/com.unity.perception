using System.Collections.Generic;
using static UnityEngine.Perception.Content.CharacterValidation;
using System.Linq;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.Content
{
    public class CharacterTooling : MonoBehaviour
    {
        /// <summary>
        /// Bool function used for testing to make sure the target character has the required 15 starting bones
        /// </summary>
        /// <param name="selection">target character selected</param>
        /// <param name="failed">Dictionary return if of Human Bones that tracks they are prsent or missing</param>
        /// <returns></returns>
        public bool CharacterRequiredBones(GameObject selection, out Dictionary<HumanBone, bool> failed)
        {
            var result = AvatarRequiredBones(selection);
            failed = new Dictionary<HumanBone, bool>();

            for (int i = 0; i < result.Count; i++)
            {
                var bone = result.ElementAt(i);
                var boneKey = bone.Key;
                var boneValue = bone.Value;

                if (boneValue != true)
                    failed.Add(boneKey, boneValue);
            }

            return failed.Count == 0;
        }

        /// <summary>
        /// Ensures there is pose data in the parent and child game objects of a character by checking for position and rotation
        /// </summary>
        /// <param name="gameObject">Target character selected</param>
        /// <param name="failedGameObjects">List of game objects that don't have nay pose data</param>
        /// <returns></returns>
        public bool CharacterPoseData(GameObject gameObject, out List<GameObject> failedGameObjects)
        {
            failedGameObjects = new List<GameObject>();

            var componentsParent = gameObject.GetComponents<Transform>();
            var componentsChild = gameObject.GetComponentsInChildren<Transform>();

            for (int p = 0; p < componentsParent.Length; p++)
            {
                var pos = componentsParent[p].transform.position;
                var rot = componentsParent[p].transform.rotation.eulerAngles;

                if (pos == null || rot == null)
                {
                    failedGameObjects.Add(componentsParent[p].gameObject);
                }
            }

            for (int c = 0; c < componentsChild.Length; c++)
            {
                var pos = componentsChild[c].transform.position;
                var rot = componentsChild[c].transform.rotation.eulerAngles;

                if (pos == null || rot == null)
                {
                    failedGameObjects.Add(componentsChild[c].gameObject);
                }
            }

            return failedGameObjects.Count == 0;
        }

        /// <summary>
        /// Bool function to make create a new prefab Character with nose and ear joints
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="drawRays"></param>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public bool CharacterCreateNose(GameObject selection, bool drawRays = false, string savePath = "Assets/")
        {
            var model = AvatarCreateNoseEars(selection, savePath, drawRays);

            if (model.name.Contains("Failed"))
            {
                GameObject.DestroyImmediate(model);
                return false;
            }
            else return true;
        }

        public bool ValidateNoseAndEars(GameObject selection)
        {
            var jointLabels = selection.GetComponentsInChildren<JointLabel>();
            var nose = false;
            var earRight = false;
            var earLeft = false;

            for (int i = 0; i < jointLabels.Length; i++)
            {
                if (jointLabels[i].name.Contains("nose"))
                    nose = true;
                if (jointLabels[i].name.Contains("earRight"))
                    earRight = true;
                if (jointLabels[i].name.Contains("earLeft"))
                    earLeft = true;
            }

            if (nose && earRight && earLeft)
                return true;
            else return false;
        }
    }
}
