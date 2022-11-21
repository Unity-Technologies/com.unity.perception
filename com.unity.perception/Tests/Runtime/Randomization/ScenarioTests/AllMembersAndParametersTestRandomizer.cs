using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.RandomizationTests.ScenarioTests
{
    [AddRandomizerMenu("")]
    public class AllMembersAndParametersTestRandomizer : Randomizer
    {
        // Members
        public bool booleanMember = false;
        public int intMember = 4;
        public uint uintMember = 2;
        public float floatMember = 5;
        public Vector2 vector2Member = new Vector2(4, 7);
        public UniformSampler unsupportedMember = new UniformSampler();

        // Parameters
        public BooleanParameter booleanParam = new BooleanParameter()
        {
            value = new ConstantSampler(1)
        };
        public FloatParameter floatParam = new FloatParameter()
        {
            value = new AnimationCurveSampler()
        };
        public IntegerParameter integerParam = new IntegerParameter()
        {
            value = new UniformSampler(-3, 7)
        };
        public Vector2Parameter vector2Param = new Vector2Parameter()
        {
            x = new ConstantSampler(2),
            y = new UniformSampler(-4, 8)
        };
        public Vector3Parameter vector3Param = new Vector3Parameter()
        {
            x = new NormalSampler(-5, 9, 4, 2),
            y = new ConstantSampler(3),
            z = new AnimationCurveSampler()
        };
        public Vector4Parameter vector4Param = new Vector4Parameter()
        {
            x = new NormalSampler(-5, 9, 4, 2),
            y = new ConstantSampler(3),
            z = new AnimationCurveSampler(),
            w = new UniformSampler(-12, 42)
        };
        public CategoricalParameter<Color> colorRgbCategoricalParam = new CategoricalParameter<Color>();
    };
}
