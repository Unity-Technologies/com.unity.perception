#if HDRP_PRESENT
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomPassDrawer(typeof(InstanceSegmentationPass))]
    public class InstanceSegmentationPassEditor : BaseCustomPassDrawer
    {
        protected override void Initialize(SerializedProperty customPass)
        {
            var targetCameraProperty = customPass.FindPropertyRelative(nameof(InstanceSegmentationPass.targetCamera));
            AddProperty(targetCameraProperty);
            AddProperty(customPass.FindPropertyRelative(nameof(InstanceSegmentationPass.targetTexture)));
            base.Initialize(customPass);
        }
    }
}
#endif
