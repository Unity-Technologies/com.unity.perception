namespace UnityEngine.Perception.Content
{
    public class ContentData : MonoBehaviour
    {
        //UV Count
        public int TryGetAssetUVCount(MeshFilter mesh)
        {
            return mesh.sharedMesh.uv.Length;
        }

        public void TryGetAssetUVCount(MeshFilter mesh, out int count, out string assetName)
        {
            assetName = mesh.name;
            count = mesh.sharedMesh.uv.Length;
        }

        //Size bounds
        public void TryGetSizeAsset(MeshRenderer mesh, out Vector3 assetSize, out string assetName)
        {
            assetName = mesh.name;
            assetSize = new Vector3(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.y);
        }

        public Vector3 TryGetSizeAsset(MeshRenderer mesh)
        {
            return new Vector3(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.y);
        }

        //Vertex Count
        public void TryGetAssetVertexCount(MeshFilter mesh, out int count, out string assetName)
        {
            assetName = mesh.name;
            count = mesh.sharedMesh.vertexCount;
        }

        public int TryGetAssetVertexCount(MeshFilter mesh)
        {
            return mesh.sharedMesh.vertexCount;
        }

        //Triangles Count for polygon count
        public void TryGetAssetPolygonCount(MeshFilter mesh, out int count, out string assetName)
        {
            assetName = mesh.name;
            count = mesh.sharedMesh.triangles.Length / 3;
        }

        public int TryGetAssetPolygonCount(MeshFilter mesh)
        {
            return mesh.sharedMesh.triangles.Length / 3;
        }

    }
}
