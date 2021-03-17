using System.Collections.Generic;
using static UnityEngine.Perception.Content.GeomertryValidation;
using static UnityEngine.Perception.Content.TextureValidation;
using static UnityEngine.Perception.Content.CharacterValidation;
using System.Linq;

namespace UnityEngine.Perception.Content
{
    public class ContentValidation : MonoBehaviour
    {
        public bool TestScale(MeshRenderer[] meshRenderers, float heightMax = 2.44f, float widthMax = 2.44f, float lengthMax = 2.44f)
        {
            var heightPass = false;
            var widthPass = false;
            var lengthPass = false;

            float heightMin = 0.01f;
            float widthMin = 0.01f;
            float lengthMin = 0.01f;

            foreach (var meshRenderer in meshRenderers)
            {
                var height = meshRenderer.bounds.size.y;
                var width = meshRenderer.bounds.size.x;
                var length = meshRenderer.bounds.size.z;

                if (height <= heightMax && height >= heightMin)
                {
                    heightPass = true;
                }

                if (width <= widthMax && width >= widthMin)
                {
                    widthPass = true;
                }

                if (length <= lengthMax && length >= lengthMin)
                {
                    lengthPass = true;
                }
            }

            if (heightPass && widthPass && lengthPass)
                return true;

            return false;
        }

        public bool TransformTest(GameObject gameObject, out List<GameObject> failedGameObjects)
        {
            /// Bounding box at Vector.Zero 
            var componentsParent = gameObject.GetComponents<Component>();
            var componentsChild = gameObject.GetComponentsInChildren<Component>();



            var posTransformTest = false;
            var rotTransformTest = false;

            failedGameObjects = new List<GameObject>();

            foreach (var com in componentsParent)
            {
                posTransformTest = false;
                rotTransformTest = false;

                if (com.GetType() == typeof(Transform))
                {
                    var rotParent = gameObject.transform.rotation;
                    var pos = com.transform.position;
                    var rot = rotParent.eulerAngles;

                    if (pos == Vector3.zero)
                    {
                        posTransformTest = true;
                    }

                    if ((rot.x > 269) && (rot.x < 271))
                        rotTransformTest = true;
                    else if ((rot.x > 89) && (rot.x < 91))
                        rotTransformTest = true;

                    if (rotParent == Quaternion.identity)
                        rotTransformTest = true;

                    if (!posTransformTest || !rotTransformTest)
                        failedGameObjects.Add(com.gameObject);
                }
            }

            foreach (var com in componentsChild)
            {
                posTransformTest = false;
                rotTransformTest = false;

                if (com.GetType() == typeof(Transform))
                {
                    var pos = com.transform.position;
                    var rotEuler = com.transform.rotation.eulerAngles;
                    var rot = com.transform.rotation;

                    if (pos == Vector3.zero)
                    {
                        posTransformTest = true;
                    }

                    if ((rotEuler.x > 269) && (rotEuler.x < 271))
                        rotTransformTest = true;
                    else if ((rotEuler.x > 89) && (rotEuler.x < 91))
                        rotTransformTest = true;

                    if (rot == Quaternion.identity)
                        rotTransformTest = true;

                    if (!posTransformTest || !rotTransformTest)
                        failedGameObjects.Add(com.gameObject);
                }
            }
            if (failedGameObjects.Count == 0)
                return true;
            return false;
        }

        public bool EmptyComponentsTest(GameObject selection)
        {
            var parentComTest = false;
            var baseComTest = false;
            var parentComponents = selection.GetComponents<Component>();

            foreach (var com in parentComponents)
            {
                var type = com.GetType();
                if (type == selection.transform.GetType() || com.name.Contains("MetaData"))
                    baseComTest = true;
                else
                    baseComTest = false;
            }

            if (parentComponents.Length <= 2)
            {
                parentComTest = true;
            }
            else
            {
                parentComTest = false;
            }

            if (parentComTest && baseComTest)
                return true;

            return false;
        }

        public bool AssetCheckPivotPoint(GameObject gameObject, out List<GameObject> failed)
        {
            failed = new List<GameObject>();

            var posParent = gameObject.transform.position;
            var componentsChild = gameObject.GetComponentsInChildren<Transform>();

            Vector3 attachmentPoint = new Vector3();

            float comparePoints = 0f;

            string[] attachmentPoints = { "base", "lamp", "backplate", "frame", "holder" };

            bool attachPointFound = false;

            for (int c = 0; c < componentsChild.Length; c++)
            {
                for (int a = 0; a < attachmentPoints.Length; a++)
                {
                    if (componentsChild[c].name == attachmentPoints[a])
                    {
                        attachmentPoint = componentsChild[c].transform.position;
                        comparePoints = Vector3.Distance(attachmentPoint, posParent);
                        attachPointFound = true;
                    }
                }
            }

            if (!attachPointFound)
            {
                attachmentPoint = Vector3.zero;
                comparePoints = Vector3.Distance(attachmentPoint, posParent);
            }

            if (comparePoints > 0f)
                failed.Add(gameObject);

            if (failed.Count == 0)
                return true;

            return false;
        }

        public bool UVDataTest(MeshFilter[] meshFilters)
        {
            var uvsNotNull = false;
            var uvsData = false;

            foreach (var mesh in meshFilters)
            {
                var uvs = mesh.sharedMesh.uv;

                if (uvs != null)
                    uvsNotNull = true;
                if (uvs.Length > 0)
                    uvsData = true;
            }

            if (uvsNotNull && uvsData)
                return true;

            return false;
        }

        public bool UVRangeTest(MeshFilter[] meshFilters)
        {
            var failedUvs = new List<Vector2>();
            var failedMeshes = new Dictionary<MeshFilter, decimal>();
            decimal uvPassPercentage = 0;

            foreach (var mesh in meshFilters)
            {
                var uvs = mesh.sharedMesh.uv;
                var uvPassed = 0;
                var uvTotal = uvs.Length;

                foreach (var uv in uvs)
                {
                    if (uv.x < 0 || uv.x > 1 && uv.y < 0 || uv.y > 1)
                    {
                        failedUvs.Add(uv);
                    }
                    else
                    {
                        uvPassed += 1;
                    }
                }

                uvPassPercentage = (decimal)uvPassed / uvTotal;
                uvPassPercentage = uvPassPercentage * 100;

                failedMeshes.Add(mesh, uvPassPercentage);

                if (uvPassPercentage < 20)
                {
                    Debug.Log(string.Format("{0} failed UV range test with a percentage of {1}", mesh.name, uvPassPercentage));
                }
            }

            if (uvPassPercentage < 20)
                return false;

            return true;
        }

        public bool UVPositiveTest(MeshFilter[] meshFilters)
        {
            var failedUvs = new List<Vector2>();
            var failedMeshes = new Dictionary<MeshFilter, decimal>();
            decimal uvPassPercentage = 0;

            foreach (var mesh in meshFilters)
            {
                var uvs = mesh.sharedMesh.uv;
                var uvPassed = 0;
                var uvTotal = uvs.Length;

                foreach (var uv in uvs)
                {
                    if (uv.x < 0 || uv.y < 0)
                    {
                        failedUvs.Add(uv);
                    }
                    else
                    {
                        uvPassed += 1;
                    }
                }

                uvPassPercentage = (decimal)uvPassed / uvTotal;
                uvPassPercentage = uvPassPercentage * 100;

                failedMeshes.Add(mesh, uvPassPercentage);
            }

            if (failedMeshes.Count > 0)
                return false;

            return true;
        }

        public bool MeshInvertedNormalsTest(MeshFilter[] meshFilters, out List<string> failedMeshFilters)
        {
            failedMeshFilters = new List<string>();
            foreach (var mesh in meshFilters)
            {
                var normals = mesh.sharedMesh.normals;

                foreach (var norm in normals)
                {
                    if (norm.normalized.magnitude < 0)
                    {
                        failedMeshFilters.Add(mesh.name);
                    }
                }
            }

            if (failedMeshFilters.Count == 0)
                return true;
            else if (failedMeshFilters.Count > 0)
                return false;

            return false;
        }

        public bool MeshOpenFaceTest(MeshFilter[] meshFilters, out List<string> failedMesh, bool drawMesh = false, bool drawEdges = false)
        {
            var edges = new List<MeshEdge>();
            failedMesh = new List<string>();
            foreach (var mesh in meshFilters)
            {
                edges = GetMeshEdges(mesh.sharedMesh, drawMesh).DetectOpenEdges(drawEdges);

                if (edges.Count != 0)
                {
                    failedMesh.Add(mesh.name);
                }
            }

            if (failedMesh.Count == 0)
                return true;

            return false;
        }

        public bool MeshDetectUnWeldedVerts(MeshFilter[] meshFilters, out List<string> failedMesh)
        {
            failedMesh = new List<string>();
            foreach (var mesh in meshFilters)
            {
                var verts = DetectUnweldedVerts(mesh.sharedMesh, 0.0001f);
                if (verts.Count != 0)
                {
                    failedMesh.Add(mesh.name);
                }
            }

            if (failedMesh.Count == 0)
                return true;

            return false;
        }

        public bool MeshDetectSpikes(MeshFilter[] meshFilters, bool DebugDraw)
        {
            var failedMesh = new List<string>();
            foreach (var mesh in meshFilters)
            {
                var edges = GetMeshEdges(mesh.sharedMesh, false).DetectMeshSpikes(DebugDraw);
                if (edges.Count != 0)
                    failedMesh.Add(mesh.name);
            }

            if (failedMesh.Count == 0)
                return true;

            return false;
        }

        public bool AssetTexelDensity(MeshFilter[] meshFilter, MeshRenderer[] meshRenderer, out List<GameObject> failedMeshes, int scale = 4, int targetResolution = 2048)
        {
            List<TexelDensity> texelDensity = new List<TexelDensity>();
            failedMeshes = new List<GameObject>();
            for (int i = 0; i < meshRenderer.Length; i++)
            {
                texelDensity = GetTexelDensity(meshFilter[i], meshRenderer[i], scale, targetResolution);

                foreach (var td in texelDensity)
                {
                    if (td.texelDensity != targetResolution / 100)
                        failedMeshes.Add(meshFilter[i].gameObject);

                    if (td.tilingValue <= scale)
                        if (!failedMeshes.Contains(meshFilter[i].gameObject))
                            failedMeshes.Add(meshFilter[i].gameObject);
                }
            }

            if (failedMeshes.Count != 0)
            {
                return false;
            }

            return true;
        }

        public bool CharacterRequiredBones(Animator animator, out Dictionary<HumanBone, bool> failed)
        {
            var result = AvatarRequiredBones(animator);
            failed = new Dictionary<HumanBone, bool>();

            for (int i = 0; i < result.Count; i++)
            {
                var bone = result.ElementAt(i);
                var boneKey = bone.Key;
                var boneValue = bone.Value;

                if (boneValue != true)
                    failed.Add(boneKey, boneValue);
            }

            if (failed.Count == 0)
                return true;

            return false;
        }

        public bool CharacterPoseData(GameObject gameObject, out List<GameObject> failedGameObjects)
        {
            failedGameObjects = new List<GameObject>();

            var componentsParent = gameObject.GetComponents<Transform>();
            var componentsChild = gameObject.GetComponentsInChildren<Transform>();

            for (int p = 0; p < componentsParent.Length; p++)
            {
                if (componentsParent[p].GetType() == typeof(Transform))
                {
                    var pos = componentsParent[p].transform.position;
                    var rot = componentsParent[p].transform.rotation.eulerAngles;

                    if (pos == null || rot == null)
                    {
                        failedGameObjects.Add(componentsParent[p].gameObject);
                    }
                }
            }

            for (int c = 0; c < componentsChild.Length; c++)
            {
                if (componentsChild[c].GetType() == typeof(Transform))
                {
                    var pos = componentsChild[c].transform.position;
                    var rot = componentsChild[c].transform.rotation.eulerAngles;

                    if (pos == null || rot == null)
                    {
                        failedGameObjects.Add(componentsChild[c].gameObject);
                    }
                }
            }

            if (failedGameObjects.Count == 0)
                return true;

            return false;
        }

        public bool CharacterCreateNose(GameObject selection, Animator animator, SkinnedMeshRenderer skinnedMeshRenderer, bool drawRays = false)
        {
            var model = AvatarCreateNose(selection, animator, skinnedMeshRenderer, drawRays);
            var childCount = model.transform.childCount;
            var nose = false;
            var earRight = false;
            var earLeft = false;

            for (int i = 0; i < childCount; i++)
            {
                if (model.transform.GetChild(i).name == "head")
                {
                    var modelHead = model.transform.GetChild(i);
                    var headChildCount = modelHead.transform.childCount;

                    for (int h = 0; h > headChildCount; h++)
                    {
                        var childName = modelHead.transform.GetChild(h).name;
                        if (childName.Contains("nose"))
                            nose = true;
                        if (childName.Contains("earRight"))
                            earRight = true;
                        if (childName.Contains("earLeft"))
                            earLeft = true;
                    }
                }
            }

            if (nose && earRight && earLeft)
                return true;

            return false;
        }
    }
}
