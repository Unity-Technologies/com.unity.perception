using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class InstanceSegmentationCrossPipelinePass : GroundTruthCrossPipelinePass
{
    public static readonly int SegmentationIdProperty = Shader.PropertyToID("_SegmentationId");
    const string k_SegmentationPassShaderName = "Perception/InstanceSegmentation";

    static ProfilerMarker s_ExecuteMarker = new ProfilerMarker("SegmentationPass_Execute");

    //Filter settings
    public LayerMask layerMask = -1;

    Shader m_SegmentationShader;
    Material m_OverrideMaterial;
    public bool reassignIds = false;
    public uint idStart = 1;
    public uint idStep = 1;
    int m_NextObjectIndex;

    Dictionary<uint, uint> m_Ids;

    public InstanceSegmentationCrossPipelinePass(Camera targetCamera, uint idStart = 1, uint idStep = 1)
        :base(targetCamera)
    {
        if (targetCamera == null)
            throw new ArgumentNullException(nameof(targetCamera));

        //Activating in the constructor allows us to get correct labeling in the first frame.
        EnsureActivated();

        this.idStart = idStart;
        this.idStep = idStep;
    }

    public override void Setup()
    {
        base.Setup();
        m_SegmentationShader = Shader.Find(k_SegmentationPassShaderName);
        var shaderVariantCollection = new ShaderVariantCollection();
        shaderVariantCollection.Add(new ShaderVariantCollection.ShaderVariant(m_SegmentationShader, PassType.ScriptableRenderPipeline));
        shaderVariantCollection.WarmUp();

        m_OverrideMaterial = new Material(m_SegmentationShader);
    }

    //Render all objects to our target RenderTexture using `overrideMaterial` to use our shader
    protected override void ExecutePass(ScriptableRenderContext renderContext, CommandBuffer cmd, Camera camera, CullingResults cullingResult)
    {
        using (s_ExecuteMarker.Auto())
        {
            cmd.ClearRenderTarget(true, true, Color.clear);
            var result = CreateRendererListDesc(camera, cullingResult, "FirstPass", 0, m_OverrideMaterial, layerMask);

            DrawRendererList(renderContext, cmd, RendererList.Create(result));
        }
    }

    public override void SetupMaterialProperties(MaterialPropertyBlock mpb, MeshRenderer meshRenderer, Labeling labeling, uint instanceId)
    {
        if (reassignIds)
        {
            //Almost all the code here interacts with shared state, so just use a simple lock for now.
            lock (this)
            {
                if (m_Ids == null)
                    m_Ids = new Dictionary<uint, uint>();

                if (!m_Ids.TryGetValue(instanceId, out var actualId))
                {
                    actualId = (uint)m_NextObjectIndex * idStep + idStart;
                    m_Ids.Add(instanceId, actualId);
                    instanceId = actualId;
                    m_NextObjectIndex++;
                }
            }
        }
        mpb.SetInt(SegmentationIdProperty, (int)instanceId);
#if PERCEPTION_DEBUG
        Debug.Log($"Assigning id. Frame {Time.frameCount} id {id}");
#endif
    }
}
