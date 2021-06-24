using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MaterialPropertyTarget
{
    Renderer,
    Material
}
[ExecuteInEditMode]
public class TestMpbPerMaterial : MonoBehaviour
{
    public MaterialPropertyTarget materialPropertyTarget = MaterialPropertyTarget.Material;
    public Color color;
    // Update is called once per frame
    void Start()
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color);
        if (materialPropertyTarget == MaterialPropertyTarget.Renderer)
            meshRenderer.SetPropertyBlock(mpb);
        else
        {
            for (int i = 0; i < meshRenderer.sharedMaterials.Length; i++)
            {
                meshRenderer.SetPropertyBlock(mpb, i);
            }
        }
    }
}
