#if HDRP_PRESENT

using UnityEditor;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Perception.Sensors.Editor
{
    [CustomPassDrawer(typeof(InstanceSegmentationPass))]
    public class InstanceSegmentationPassEditor : BaseCustomPassDrawer
    {
        protected override void Initialize(SerializedProperty customPass)
        {
            var targetCameraProperty = customPass.FindPropertyRelative(nameof(GroundTruthPass.targetCamera));
            AddProperty(targetCameraProperty);
            AddProperty(customPass.FindPropertyRelative(nameof(InstanceSegmentationPass.targetTexture)));
            AddProperty(customPass.FindPropertyRelative(nameof(InstanceSegmentationPass.reassignIds)));
            AddProperty(customPass.FindPropertyRelative(nameof(InstanceSegmentationPass.idStart)));
            AddProperty(customPass.FindPropertyRelative(nameof(InstanceSegmentationPass.idStep)));
            base.Initialize(customPass);
        }
    }
}
#endif
