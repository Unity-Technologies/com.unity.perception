using UnityEngine;

public interface IGroundTruthGenerator
{
    void SetupMaterialProperties(MaterialPropertyBlock mpb, MeshRenderer meshRenderer, Labeling labeling, uint instanceId);
}
