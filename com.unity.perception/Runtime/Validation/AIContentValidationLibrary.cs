using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Perception.Content
{
    public static class GeomertryValidation
    {
        public struct MeshEdge
        {
            public Vector3 v1;
            public Vector3 v2;
            public MeshEdge(Vector3 v1, Vector3 v2)
            {
                this.v1 = v1;
                this.v2 = v2;
            }
        }

        public struct MeshTrianlge
        {
            public MeshEdge e1;
            public MeshEdge e2;
            public MeshEdge e3;
            public MeshTrianlge(MeshEdge e1, MeshEdge e2, MeshEdge e3)
            {
                this.e1 = e1;
                this.e2 = e2;
                this.e3 = e3;
            }
        }

        public static List<MeshEdge> GetMeshEdges(Mesh mesh, bool drawMesh)
        {
            List<MeshEdge> result = new List<MeshEdge>();

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                var v1 = mesh.vertices[mesh.triangles[i]];
                var v2 = mesh.vertices[mesh.triangles[i + 1]];
                var v3 = mesh.vertices[mesh.triangles[i + 2]];
                result.Add(new MeshEdge(v1, v2));
                result.Add(new MeshEdge(v2, v3));
                result.Add(new MeshEdge(v3, v1));

                if (drawMesh)
                {
                    Debug.DrawLine(v1, v2, Color.green, 30f);
                    Debug.DrawLine(v1, v3, Color.green, 30f);
                    Debug.DrawLine(v2, v3, Color.green, 30f);
                }
            }
            return result;
        }

        public static List<MeshTrianlge> CreateTrianlges(Mesh mesh, bool drawMesh)
        {
            List<MeshTrianlge> result = new List<MeshTrianlge>();

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                var v1 = mesh.vertices[mesh.triangles[i]];
                var v2 = mesh.vertices[mesh.triangles[i + 1]];
                var v3 = mesh.vertices[mesh.triangles[i + 2]];

                result.Add(new MeshTrianlge(new MeshEdge(v1, v2), new MeshEdge(v2, v3), new MeshEdge(v3, v1)));

                if (drawMesh)
                {
                    Debug.DrawLine(v1, v2, Color.green, 30f);
                    Debug.DrawLine(v1, v3, Color.green, 30f);
                    Debug.DrawLine(v2, v3, Color.green, 30f);
                }
            }
            return result;
        }

        public static List<MeshEdge> DetectOpenEdges(this List<MeshEdge> Edges, bool drawMesh)
        {
            List<MeshEdge> result = Edges;
            for (int i = result.Count - 1; i > 0; i--)
            {
                for (int n = i - 1; n >= 0; n--)
                {
                    if (result[i].v1 == result[n].v2 && result[i].v2 == result[n].v1)
                    {
                        // shared edge so remove both
                        result.RemoveAt(i);
                        result.RemoveAt(n);
                        i--;
                        break;
                    }
                }
            }

            if (drawMesh)
            {
                if (result.Count > 0)
                {
                    for (int e = 0; e < result.Count; e++)
                    {
                        Debug.DrawLine(result[e].v1, result[e].v2, Color.blue, 30f);
                    }
                }
            }

            return result;
        }

        public static List<MeshEdge> SortEdges(this List<MeshEdge> Edges)
        {
            List<MeshEdge> result = new List<MeshEdge>(Edges);
            for (int i = 0; i < result.Count - 2; i++)
            {
                MeshEdge e = result[i];
                for (int n = i + 1; n < result.Count; n++)
                {
                    MeshEdge a = result[n];
                    if (e.v2 == a.v1)
                    {
                        if (n == i + 1)
                            break;
                        result[n] = result[i + 1];
                        result[i + 1] = a;
                        break;
                    }
                }
            }
            return result;
        }

        public static List<Vector3> DetectUnweldedVerts(Mesh mesh, float distance)
        {
            List<Vector3> result = mesh.vertices.ToList();

            for (int i = result.Count - 1; i > 0; i--)
            {
                for (int p = i - 1; p >= 0; p--)
                {
                    var m = mesh.vertices[i];
                    var a = mesh.vertices[p];
                    var factor = Vector3.Distance(m, a);
                    if (factor > distance || factor != 0)
                    {
                        result.Remove(m);
                    }
                }
            }

            return result;
        }

        public static List<MeshEdge> DetectMeshSpikes(this List<MeshEdge> Edges, bool DebugDraw)
        {
            /// TODO: Doesn't support simple object i.e. a cube because cube all edges z's outside of the bounds
            /// Can possibly do this by checking each triangle and the angles within to see if it contains a 90 degree
            
            List<MeshEdge> result = Edges;
            List<MeshEdge> spikes = new List<MeshEdge>();

            for (int i = 0; i < result.Count; i++)
            {
                var v1 = result[i].v1;
                var v2 = result[i].v2;

                var zDiff = v1.z - v2.z;
                var yDiff = v1.y - v2.y;
                var xDiff = v1.x - v2.x;

                if (zDiff > 0.0500 || zDiff < -0.0500)
                {
                    spikes.Add(result[i]);
                    if(DebugDraw)
                        Debug.DrawLine(v1, v2, Color.red, 30f);
                }
            }

            return spikes;
        }
    }

    public static class TextureValidation
    {
        public struct TexelDensity
        {
            public double texelDensity;
            public double tilingValue;

            public TexelDensity(double texelDensity, double tilingValue)
            {
                this.texelDensity = texelDensity;
                this.tilingValue = tilingValue;
            }
        }

        public static List<TexelDensity> GetTexelDensity(MeshFilter meshFilter, MeshRenderer meshRenderer, float scale, int targetResolution)
        {
            // Correct TD for an object is meter x pixel per meter / texture Resolution = tiling value 
            // Find TD is pixel / 100cm = TD
            List <TexelDensity> result = new List<TexelDensity>();
            List<Vector2> uvs = meshFilter.sharedMesh.uv.ToList();
            
            var assetWorldSize = meshRenderer.bounds.size;
            
            // Place a texture, although might need to just make the texture 2048
            Texture2D testTexture = new Texture2D(targetResolution, targetResolution);
            testTexture.wrapMode = TextureWrapMode.Repeat;
            testTexture.filterMode = FilterMode.Bilinear;
            testTexture.anisoLevel = 1;

            AssetDatabase.CreateAsset(testTexture, "Assets/testTexture.renderTexture");
            var path = AssetDatabase.GetAssetPath(testTexture);
            var test = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

            if(meshRenderer.sharedMaterial != null)
                meshRenderer.sharedMaterial.mainTexture = test;

            int textureheight = meshRenderer.sharedMaterial.mainTexture.height;
            int texturewidth = meshRenderer.sharedMaterial.mainTexture.width;
            Vector2 uvSize = new Vector2();
            float uvScale = 0;

            // Scale UV's to scale input
            for (int i = 0; i < uvs.Count; i++)
            {
                // UVs support a texel density of 2048x2048px to 4'0''x4'0'' 
                uvs[i] = new Vector2(uvs[i].x * scale, uvs[i].y * scale);
                if (i == 0)
                {
                    uvSize = uvs[i];
                }
                else
                {
                    if (uvs[i].x > uvs[i - 1].x)
                        uvSize.x = uvs[i].x;
                    if (uvs[i].y > uvs[i - 1].y)
                        uvSize.y = uvs[i].y;
                }
            }

            // Calculate tile map to see if it is supported as whole number
            // Calculate TD and check to make sure it is supported version of 20.48 or other rez
            uvScale = uvSize.x;
            double texelDensity = textureheight / 100.0;
            double tilingValue = uvScale * (texelDensity * 100) / textureheight;

            result.Add(new TexelDensity(texelDensity, tilingValue));

            return result;
        }
    }

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
                    }

                    if (earRightPosition != Vector3.zero)
                    {
                        var earRight = new GameObject();
                        earRight.transform.position = earRightPosition;
                        earRight.name = "earRight";
                        earRight.transform.SetParent(child);
                    }

                    if (earLeftPosition != Vector3.zero)
                    {
                        var earLeft = new GameObject();
                        earLeft.transform.position = earLeftPosition;
                        earLeft.name = "earLeft";
                        earLeft.transform.SetParent(child);
                    }
                }
            }

            var model = PrefabUtility.SaveAsPrefabAsset(root, "Assets/" + root.name + ".prefab");

            return model;
        }
    }
}
