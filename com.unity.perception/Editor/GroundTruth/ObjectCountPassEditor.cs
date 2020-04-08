#if HDRP_PRESENT

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Perception.Sensors.Editor
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
