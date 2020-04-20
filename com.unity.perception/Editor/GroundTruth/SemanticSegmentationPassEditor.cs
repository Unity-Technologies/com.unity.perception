#if HDRP_PRESENT

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomPassDrawer(typeof(SemanticSegmentationPass))]
    class SemanticSegmentationPassEditor : BaseCustomPassDrawer
    {
        protected override void Initialize(SerializedProperty customPass)
        {
            AddProperty(customPass.FindPropertyRelative(nameof(SemanticSegmentationPass.targetCamera)));
            AddProperty(customPass.FindPropertyRelative(nameof(SemanticSegmentationPass.targetTexture)));
            AddProperty(customPass.FindPropertyRelative(nameof(SemanticSegmentationPass.labelingConfiguration)));
            base.Initialize(customPass);
        }
    }
}
#endif
