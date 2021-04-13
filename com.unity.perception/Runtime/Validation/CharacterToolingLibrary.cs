using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Perception.GroundTruth;

namespace UnityEngine.Perception.Content
{
    public static class CharacterValidation
    {
        public static string[] RequiredBones =
        {
            "Head",
            "Hips",
            "Spine",
            "LeftUpperArm",
            "LeftLowerArm",
            "LeftHand",
            "RightUpperArm",
            "RightLowerArm",
            "RightHand",
            "LeftUpperLeg",
            "LeftLowerLeg",
            "LeftFoot",
            "RightUpperLeg",
            "RightLowerLeg",
            "RightFoot",
        };

        public static Dictionary<HumanBone, bool> AvatarRequiredBones(Animator animator)
        {
            var result = new Dictionary<HumanBone, bool>();
            var human = animator.avatar.humanDescription.human;
            var totalBones = 0;

            for(int h = 0; h < human.Length; h++)
            {
                for (int b = 0; b < RequiredBones.Length; b++)
                {
                    if(human[h].humanName == RequiredBones[b])
                    {
                        var bone = new HumanBone();

                        if (human[h].boneName != null)
                        {
                            bone.boneName = human[h].boneName;
                            totalBones = totalBones = +1;
                            result.Add(bone, true);
                        }
                        else
                        {
                            result.Add(bone, false);
                        }
                    }
                }
            }

            return result;
        }

        public static GameObject AvatarCreateNose (GameObject selection, Animator animator, SkinnedMeshRenderer skinnedMeshRenderer, bool drawRays = false)
        {
            var human = animator.avatar.humanDescription.human;
            var skeleton = animator.avatar.humanDescription.skeleton;
            var verticies = skinnedMeshRenderer.sharedMesh.vertices;

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var leftEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            var rightEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var faceCenter = Vector3.zero;
            var earCenter = Vector3.zero;
            var nosePos = Vector3.zero;
            var earRightPos = Vector3.zero;
            var earLeftPos = Vector3.zero;
            var distanceCheck = 1f;

            var eyeDistance = Vector3.Distance(leftEye.position, rightEye.position);
            var directionLeft = Quaternion.AngleAxis(-45, -leftEye.right) * -leftEye.up;
            var directionRight = Quaternion.AngleAxis(-45, rightEye.right) * -rightEye.up;

            var rayHead = new Ray();
            var rayLeftEye = new Ray();
            var rayRightEye = new Ray();
            var noseRayFor = new Ray();
            var rayNoseBack = new Ray();
            var rayEarLeft = new Ray();
            var rayEarRight = new Ray();

            var foundRightEar = false;
            var foundLeftEar = false;

            rayLeftEye.origin = leftEye.position;
            rayLeftEye.direction = directionLeft * eyeDistance;

            rayRightEye.origin = rightEye.position;
            rayRightEye.direction = directionRight * eyeDistance;

            for (double i = 0; i < distanceCheck; i += 0.01)
            {
                var point = Convert.ToSingle(i);
                var pointR = rayRightEye.GetPoint(point);
                var pointL = rayLeftEye.GetPoint(point);

                var distanceX = Math.Abs(pointR.x - pointL.x);
                var distanceY = Math.Abs(pointR.y - pointL.y);

                if (distanceX < 0.01 && distanceY < 0.01 )
                {
                    faceCenter = pointR;
                }
            }
            
            rayHead.origin = head.position;
            rayHead.direction = Vector3.up * distanceCheck;
                        
            noseRayFor.origin = faceCenter;
            noseRayFor.direction = Vector3.forward * distanceCheck;
            
            rayNoseBack.origin = faceCenter;
            rayNoseBack.direction = Vector3.back * distanceCheck;

            for (double i = 0; i < distanceCheck; i += 0.01)
            {
                var point = Convert.ToSingle(i);
                var pointH = rayHead.GetPoint(point);
                var pointF = rayNoseBack.GetPoint(point);

                var distanceZ = Math.Abs(pointH.z - pointF.z);

                if (distanceZ < 0.01)
                {
                    earCenter = pointF;
                }
            }

            for (int v = 0; v < verticies.Length; v++)
            {
                for (double c = eyeDistance / 2; c < distanceCheck; c += 0.001)
                {
                    var point = Convert.ToSingle(c);
                    var pointNoseRay = noseRayFor.GetPoint(point);
                    var pointVert = verticies[v];
                    var distHeadZ = Math.Abs(pointNoseRay.z - pointVert.z);
                    var distHeadX = Math.Abs(pointNoseRay.x - pointVert.x);

                    if (distHeadZ < 0.0001 && distHeadX < 0.001)
                    {
                        nosePos = pointNoseRay;
                        Debug.Log("Found Nose: " + nosePos);
                    }

                    if(nosePos == Vector3.zero)
                    {
                        if (distHeadZ < 0.001 && distHeadX < 0.001)
                        {
                            nosePos = pointNoseRay;
                            Debug.Log("Found Nose: " + nosePos);
                        }
                    }
                }
            }

            
            rayEarRight.origin = earCenter;
            rayEarRight.direction = Vector3.right * distanceCheck;
            
            rayEarLeft.origin = earCenter;
            rayEarLeft.direction = Vector3.left * distanceCheck;
            
            for (int v = 0; v < verticies.Length; v++)
            {
                for (double c = eyeDistance / 2; c < distanceCheck; c += 0.001)
                {
                    var point = Convert.ToSingle(c);
                    var pointEarRight = rayEarRight.GetPoint(point);
                    var pointEarLeft = rayEarLeft.GetPoint(point);
                    var pointVert = verticies[v];

                    var distEarRightY = Math.Abs(pointEarRight.y - pointVert.y);
                    var distEarRightX = Math.Abs(pointEarRight.x - pointVert.x);

                    var distEarLeftY = Math.Abs(pointEarLeft.y - pointVert.y);
                    var distEarLeftX = Math.Abs(pointEarLeft.x - pointVert.x);

                    if (distEarRightY < 0.001 && distEarRightX < 0.0001)
                    {
                        earRightPos = pointEarRight;
                        foundRightEar = true;
                    }

                    if (distEarLeftY < 0.001 && distEarLeftX < 0.0001)
                    {
                        earLeftPos = pointEarLeft;
                        foundLeftEar = true;
                    }

                    if (!foundRightEar)
                    {
                        if (distEarRightY < 0.002 && distEarRightX < 0.001)
                        {
                            earRightPos = pointEarRight;
                        }
                    }

                    if (!foundLeftEar)
                    {
                        if (distEarLeftY < 0.002 && distEarLeftX < 0.001)
                        {
                            earLeftPos = pointEarLeft;
                        }
                    }
                }
            }

            var earLeftCheck = Vector3.Distance(earLeftPos, earCenter);
            var earRightCheck = Vector3.Distance(earRightPos, earCenter);
            var eyeCenterDistance = eyeDistance + 0.01f;

            if (earLeftCheck > eyeCenterDistance)
            {
                earLeftPos.x = (-eyeDistance);
            }

            if (earRightCheck > eyeCenterDistance)
            {
                earRightPos.x = eyeDistance;
            }

            if(drawRays)
            {
                DebugDrawRays(30f, distanceCheck, rightEye, leftEye, head, rayRightEye, rayLeftEye, faceCenter, earCenter);
            }

            return CreateNewCharacterPrefab(selection, nosePos, earRightPos, earLeftPos);
        }

        public static void DebugDrawRays(float duration, float distanceCheck, Transform rightEye, Transform leftEye, Transform head,Ray rayRightEye, Ray rayLeftEye, Vector3 faceCenter, Vector3 earCenter)
        {
            Debug.DrawLine(rightEye.position, leftEye.position, Color.magenta, duration);
            Debug.DrawRay(rayLeftEye.origin, rayLeftEye.direction, Color.green, duration);
            Debug.DrawRay(rayRightEye.origin, rayRightEye.direction, Color.blue, duration);
            Debug.DrawRay(faceCenter, Vector3.forward * distanceCheck, Color.cyan, duration);
            Debug.DrawRay(faceCenter, Vector3.back, Color.cyan, duration);
            Debug.DrawRay(head.position, Vector3.up, Color.red, duration);
            Debug.DrawRay(earCenter, Vector3.right, Color.blue, duration);
            Debug.DrawRay(earCenter, Vector3.left, Color.green, duration);
        }

        public static GameObject CreateNewCharacterPrefab(GameObject selection, Vector3 nosePosition, Vector3 earRightPosition, Vector3 earLeftPosition)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(selection);
            List<Transform> children = new List<Transform>();
            root.GetComponentsInChildren(children);

            foreach(var child in children)
            {
                if (child.name == "head")
                {
                    if (nosePosition != Vector3.zero)
                    {
                        var nose = new GameObject();
                        nose.transform.position = nosePosition;
                        nose.name = "nose";
                        nose.transform.SetParent(child);

                        AddJointLabel(nose);
                    }

                    if (earRightPosition != Vector3.zero)
                    {
                        var earRight = new GameObject();
                        earRight.transform.position = earRightPosition;
                        earRight.name = "earRight";
                        earRight.transform.SetParent(child);

                        AddJointLabel(earRight);
                    }

                    if (earLeftPosition != Vector3.zero)
                    {
                        var earLeft = new GameObject();
                        earLeft.transform.position = earLeftPosition;
                        earLeft.name = "earLeft";
                        earLeft.transform.SetParent(child);

                        AddJointLabel(earLeft);
                    }
                }
            }

            var model = PrefabUtility.SaveAsPrefabAsset(root, "Assets/" + root.name + ".prefab");

            return model;
        }

        private static void AddJointLabel(GameObject gameObject)
        {
            var jointLabel = gameObject.AddComponent<JointLabel>();
            var data = new JointLabel.TemplateData();

            data.label = gameObject.name;
            jointLabel.templateInformation.Add(data);
        }
    }
}
