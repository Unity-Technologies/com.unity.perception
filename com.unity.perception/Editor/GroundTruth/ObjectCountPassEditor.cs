#if HDRP_PRESENT

using System;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomPassDrawer(typeof(ObjectCountPass))]
    public class ObjectCountPassEditor : BaseCustomPassDrawer
    {
        protected override void Initialize(SerializedProperty customPass)
        {
            AddProperty(customPass.FindPropertyRelative(nameof(GroundTruthPass.targetCamera)));
            AddProperty(customPass.FindPropertyRelative(nameof(ObjectCountPass.SegmentationTexture)));
            AddProperty(customPass.FindPropertyRelative(nameof(ObjectCountPass.LabelingConfiguration)));
            base.Initialize(customPass);
        }
    }
}
#endif
