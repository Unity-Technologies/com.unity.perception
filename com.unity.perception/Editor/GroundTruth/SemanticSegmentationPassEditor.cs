#if HDRP_PRESENT

using System;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomPassDrawer(typeof(SemanticSegmentationPass))]
    class SemanticSegmentationPassEditor : BaseCustomPassDrawer
    {
        protected override void Initialize(SerializedProperty customPass)
        {
            AddProperty(customPass.FindPropertyRelative(nameof(SemanticSegmentationPass.targetCamera)));
            AddProperty(customPass.FindPropertyRelative(nameof(SemanticSegmentationPass.targetTexture)));
            AddProperty(customPass.FindPropertyRelative(nameof(SemanticSegmentationPass.semanticSegmentationLabelConfig)));
            base.Initialize(customPass);
        }
    }
}
#endif
